using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.Swift;
using System.Text.Json;

namespace AzulLambda
{
    public class AzulNode
    {
        public AzulNode? Parent { get; }
        public List<AzulNode> Children { get; }
        public Move? Action { get; }
        public GameState GameState { get; }
        public bool hasExpanded { get; set; } = false;
        public readonly Dictionary<int, double> rewards;

        private readonly UpperConfidenceBoundsForNodes _bandit;
        private readonly int _agentId;
        private readonly int _enemyId;
        private readonly double _discountFactor;
        private readonly double _alpha;
        private int _visitCount;

        public AzulNode(
            AzulNode? parent,
            GameState gameState,
            UpperConfidenceBoundsForNodes bandit,
            Dictionary<int, double> setRewards,
            Move? action,
            int agentId,
            double discountFactor,
            double alpha,
            GameState? prevState,
            bool useMinMax)
        {
            Parent = parent;
            GameState = gameState;
            _bandit = bandit;
            rewards = setRewards;
            Action = action;
            _agentId = agentId;
            _enemyId = agentId == 0 ? 1 : 0;
            _discountFactor = discountFactor;
            _alpha = alpha;
            Children = new List<AzulNode>();
            _visitCount = 0;
        }


        public bool IsTerminal()
        {
            // If there are no tiles left in any factory, the round is over and we should not simulate further
            return GameState.Factories.All(factory => factory.Tiles.Count == 0);
        }

        public bool IsFullyExpanded()
        {
            return Children.Count == new AzulSimulator(GameState).GenerateLegalActions().Count;
        }

        public bool ShouldExpand()
        {
            return !hasExpanded;
        }

        public AzulNode SelectChild()
        {
            double bestValue = double.NegativeInfinity;
            AzulNode? bestChild = null;

            foreach (var child in Children)
            {
                double ucbValue = _bandit.Calculate(child.rewards[_agentId], child._visitCount, _visitCount);
                if (ucbValue > bestValue)
                {
                    bestValue = ucbValue;
                    bestChild = child;
                }
            }

            return bestChild ?? throw new InvalidOperationException("No valid child found.");
        }

        public void Expand()
        {
            if (hasExpanded)
            {
                return; // Already expanded
            }

            var legalActions = new AzulSimulator(GameState).GenerateLegalActions();
            foreach (var action in legalActions)
            {
                // Use AzulSimulator to simulate the new game state using MakeMove
                var copiedGameState = CloneGameState(GameState); // Clone the current game state to avoid modifying the original
                var simulator = new AzulSimulator(copiedGameState);

                // Apply the action using MakeMove
                simulator.MakeMove(_agentId, action.FactoryId, action.Color, action.PatternLine, out _);
                var newGameState = simulator.GameState;

                // Create a new node with the updated game state
                var newNode = new AzulNode(
                    this,
                    newGameState,
                    _bandit,
                    new Dictionary<int, double>{ { 0, 0 }, { 1, 0 } },
                    action,
                    _enemyId,
                    _discountFactor,
                    _alpha,
                    GameState,
                    false
                );

                Children.Add(newNode);
                hasExpanded = true; // Mark this node as expanded
            }
        }

        // Helper method to clone the GameState
        private GameState CloneGameState(GameState original)
        {
            // Use a deep copy mechanism to clone the GameState
            return JsonSerializer.Deserialize<GameState>(
                JsonSerializer.Serialize(original)
            ) ?? throw new InvalidOperationException("Failed to clone GameState.");
        }

        public Dictionary<int, double> Simulate()
        {
            var depth = 0;
            var copiedGameState = CloneGameState(GameState);
            var simulator = new AzulSimulator(copiedGameState);
            var startingRoundNumber = GameState.Round;
            var gameOver = false;
            var currentPlayer = GameState.CurrentPlayerIndex;

            //Done if all factories are empty, simming to end of game is too slow
            while (!(simulator.GameState.Round > startingRoundNumber) && !gameOver)
            {
                var actions = simulator.GenerateLegalActions();
                // Randomly select an action
                var randomAction = actions[new Random().Next(actions.Count)];

                // Apply the action using MakeMove
                simulator.MakeMove(currentPlayer, randomAction.FactoryId, randomAction.Color, randomAction.PatternLine, out gameOver);
                depth++;
                currentPlayer = (currentPlayer + 1) % 2; // Switch player
            }

            var cumulativeRewards = new Dictionary<int, double>
            {
                { _agentId, simulator.GameState.Players[_agentId].Score },
                { _enemyId, simulator.GameState.Players[_enemyId].Score }
            };

            return cumulativeRewards;
        }

        public void Backpropagate(Dictionary<int, double> rewardToProp)
        {
            _visitCount++;

            rewards[_agentId] += _alpha * rewardToProp[_agentId];
            rewards[_enemyId] += _alpha * rewardToProp[_enemyId];

            if (Parent != null)
            {
                var discountedReward = new Dictionary<int, double>
                {
                    { _agentId, rewardToProp[_agentId] * _discountFactor },
                    { _enemyId, rewardToProp[_enemyId] * _discountFactor }
                };
                Parent.Backpropagate(discountedReward);
            }
        }

        public double GetQ(int perspectivePlayerId, double aggressionFactor)
        {
            var rewardScore = rewards[perspectivePlayerId] - aggressionFactor * rewards[perspectivePlayerId];
            return _visitCount == 0 ? 0 : rewardScore / _visitCount;
        }
    }
}