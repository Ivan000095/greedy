using System;
using System.Collections.Generic;

namespace GreedyBestFirstSearchGUI
{
    public class NetworkNode
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsSelected { get; set; }

        // Vecinos: Nodo adyacente y la latencia real en milisegundos
        public Dictionary<NetworkNode, double> Neighbors { get; } = new Dictionary<NetworkNode, double>();

        public NetworkNode(string id, string label, float x, float y)
        {
            Id = id;
            Label = label;
            X = x;
            Y = y;
        }

        public void AddNeighbor(NetworkNode target, double latencyMs)
        {
            Neighbors[target] = latencyMs;
            target.Neighbors[this] = latencyMs; // Enlace bidireccional
        }

        /// <summary>
        /// Heurística h(n): Estimación de latencia basada en distancia geográfica (velocidad de propagación en fibra óptica).
        /// </summary>
        public double EstimateLatencyTo(NetworkNode target)
        {
            double dx = X - target.X;
            double dy = Y - target.Y;
            double distanceKm = Math.Sqrt(dx * dx + dy * dy);
            // Estimación física: ~5 ms por cada 100 km en línea recta
            return Math.Round(distanceKm * 0.05, 1);
        }
    }

    public class PacketSearchState
    {
        public NetworkNode Node { get; }
        public PacketSearchState? Parent { get; }
        public double HeuristicLatency { get; }  // h(n)
        public double RealCumulativeLatency { get; } // g(n) acumulada

        public PacketSearchState(NetworkNode node, PacketSearchState? parent, double heuristicLatency, double realCumulativeLatency)
        {
            Node = node;
            Parent = parent;
            HeuristicLatency = heuristicLatency;
            RealCumulativeLatency = realCumulativeLatency;
        }
    }
}
