namespace AzulLambda
{
    public class UpperConfidenceBoundsForNodes
    {
        private readonly double _explorationConstant;

        public UpperConfidenceBoundsForNodes(double explorationConstant)
        {
            _explorationConstant = explorationConstant;
        }

        public double Calculate(double qValue, int visitCount, int parentVisitCount)
        {
            if (visitCount == 0)
            {
                // Encourage exploration for unvisited nodes
                return double.PositiveInfinity;
            }

            // UCB formula: Q + C * sqrt(log(N) / n)
            double exploitation = qValue;
            double exploration = _explorationConstant * Math.Sqrt(Math.Log(parentVisitCount) / visitCount);
            return exploitation + exploration;
        }
    }
}