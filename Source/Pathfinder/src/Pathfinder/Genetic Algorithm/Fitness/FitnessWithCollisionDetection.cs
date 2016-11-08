﻿using Pathfinder.Abstraction;
using System.Linq;
using static System.Math;

namespace Pathfinder.Fitness
{
    public class FitnessWithCollisionDetection : IFitness
    {
        IHeuristic Heuristic;
        GASettings gasettings;

        public FitnessWithCollisionDetection()
        {
            Heuristic = Program.Settings.GetHeuristic();
            gasettings = Program.GASettings;
        }
        public double Calc(IGenome genome)
        {
            var _endNode = genome.Map.EndNode;
            var lastnode = genome.ListNodes.Last();
            var startnode = genome.ListNodes.First();

            var HeuristicMaxDistance = Heuristic.Calc(Abs(startnode.X - _endNode.X), Abs(startnode.Y - _endNode.Y));
            var HeuristicValue = Heuristic.Calc(Abs(lastnode.X - _endNode.X), Abs(lastnode.Y - _endNode.Y));

            var penalty = (double)0;
            if (lastnode.Collision)
                penalty = gasettings.Penalty * (HeuristicValue / HeuristicMaxDistance);

            return penalty + HeuristicValue;
        }
    }
}
