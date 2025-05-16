using System;
using System.Collections.Generic;
using AzulLambda;

namespace AzulLambda
{
    public class MCTSAgent
    {
        private readonly int _id;
        private readonly MCTS _mcts;
        private readonly UpperConfidenceBoundsForNodes _bandit;

        private readonly double ExplorationConstant = 1 / Math.Sqrt(2);
        private const double DiscountFactor = 1;
        private const double LearningTime = 0.9;
        private const double LearningRate = 0.2;
        private const double AggressionFactor = 0.2;

        public MCTSAgent(int id)
        {
            _id = id;
            _bandit = new UpperConfidenceBoundsForNodes(ExplorationConstant);
            _mcts = new MCTS();
        }

        public Move SelectAction(List<Move> actions, GameState gameState)
        {
            if (actions.Count == 1)
            {
                // If there's only one action, return it directly.
                return actions[0];
            }

            // Create the root node for MCTS
            var rootNode = new AzulNode(null, gameState, _bandit, new Dictionary<int, double> { { 0, 0 }, { 1, 0 } }, null, _id, DiscountFactor, LearningRate, null, false);

            // Run MCTS
            var doneNode = _mcts.Run(LearningTime, rootNode);

            // Find the best child node
            AzulNode? bestChild = null;
            double bestQ = double.NegativeInfinity;

            foreach (var child in doneNode.Children)
            {
                double childQ = child.GetQ(_id, AggressionFactor);
                if (childQ > bestQ)
                {
                    bestChild = child;
                    bestQ = childQ;
                }
            }

            return bestChild?.Action ?? throw new InvalidOperationException("No valid action found.");
        }
    }
}