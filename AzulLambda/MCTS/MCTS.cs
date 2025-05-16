using System.Diagnostics;

namespace AzulLambda
{
    public class MCTS
    {
        public AzulNode Run(double timeout, AzulNode rootNode)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed.TotalSeconds < timeout)
            {
                // Step 1: Selection
                var selectedNode = Select(rootNode);

                // Step 2: Expansion
                var expandedNode = Expand(selectedNode);

                // Step 3: Simulation
                var reward = Simulate(expandedNode);

                // Step 4: Backpropagation
                Backpropagate(expandedNode, reward);
            }

            stopwatch.Stop();
            return rootNode;
        }

        private AzulNode Select(AzulNode node)
        {
            // If node should be expanded, select it, or if it is terminal, return it
            while (node.ShouldExpand() || node.IsTerminal())
            {
                return node;
            }
            
            // Otherwise, select the child with the highest UCB value
            return node.SelectChild(); ;
        }

        private AzulNode Expand(AzulNode node)
        {
            node.Expand();
            return node;
        }

        private Dictionary<int, double> Simulate(AzulNode node)
        {
            return node.Simulate();
        }

        private void Backpropagate(AzulNode node, Dictionary<int, double> reward)
        {
            node.Backpropagate(reward);        
        }
    }
}