using System;
using System.Collections.Generic;

namespace GreedyBestFirstSearchGUI
{
    public class AirportNode
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsSelected { get; set; }

        // Vecinos: Aeropuerto conectado y distancia de vuelo real en kilómetros (o tiempo real en horas)
        public Dictionary<AirportNode, double> Neighbors { get; } = new Dictionary<AirportNode, double>();

        public AirportNode(string id, string label, float x, float y)
        {
            Id = id;
            Label = label;
            X = x;
            Y = y;
        }

        public void AddNeighbor(AirportNode target, double distanceKm)
        {
            Neighbors[target] = distanceKm;
            target.Neighbors[this] = distanceKm; // Ruta aérea bidireccional
        }

        /// <summary>
        /// Heurística h(n): Estimación de distancia ortodrómica / en línea recta (vuelo de pájaro) hacia el aeropuerto destino.
        /// Escala cartográfica: 1 unidad visual = ~10 km.
        /// </summary>
        public double EstimateDistanceTo(AirportNode target)
        {
            double dx = X - target.X;
            double dy = Y - target.Y;
            double directDistKm = Math.Sqrt(dx * dx + dy * dy) * 10.0;
            return Math.Round(directDistKm, 0);
        }
    }

    public class FlightSearchState
    {
        public AirportNode Node { get; }
        public FlightSearchState? Parent { get; }
        public double HeuristicDistance { get; } // h(n) en km
        public double RealCumulativeDistance { get; } // g(n) en km recorridos

        public FlightSearchState(AirportNode node, FlightSearchState? parent, double heuristicDistance, double realCumulativeDistance)
        {
            Node = node;
            Parent = parent;
            HeuristicDistance = heuristicDistance;
            RealCumulativeDistance = realCumulativeDistance;
        }
    }
}
