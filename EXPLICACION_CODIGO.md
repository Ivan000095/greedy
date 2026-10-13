# Explicacion del Algoritmo Greedy Best-First Search a Nivel de Codigo

Este documento detalla la implementacion tecnica y arquitectura de codigo del algoritmo Greedy Best-First Search (GBFS) en C# (.NET 8), tomando como base las clases y metodos contenidos en el proyecto.

---

## 1. Estructura de Clases del Modelo

El modelo de datos se encuentra definido en el archivo `NetworkModels.cs` y consta de dos entidades principales:

### 1.1. Clase `AirportNode` (Grafo / Espacio de Estados)

Representa cada vertice del grafo. Almacena las coordenadas bidimensionales para el calculo heuristico y un diccionario con sus aristas adyacentes.

```csharp
public class AirportNode
{
    public string Id { get; set; }
    public string Label { get; set; }
    public float X { get; set; }
    public float Y { get; set; }

    // Diccionario de adyacencia: Aeropuerto vecino -> Distancia real de vuelo (km)
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
        target.Neighbors[this] = distanceKm; // Grafo no dirigido (bidireccional)
    }

    // Calculo de la funcion heuristica h(n)
    public double EstimateDistanceTo(AirportNode target)
    {
        double dx = X - target.X;
        double dy = Y - target.Y;
        double directDistKm = Math.Sqrt(dx * dx + dy * dy) * 10.0;
        return Math.Round(directDistKm, 0);
    }
}
```

#### Aspectos Clave de Implementacion:
- `Neighbors`: Se implementa mediante un `Dictionary<AirportNode, double>` para permitir busquedas directas de aristas en tiempo promedio O(1).
- `EstimateDistanceTo`: Es la funcion $h(n)$. Aplica la formula de distancia euclidiana multiplicada por un factor de escala cartografico.

---

### 1.2. Clase `FlightSearchState` (Nodo del Arbol de Busqueda)

Representa un nodo dentro del espacio de busqueda generado durante la ejecucion. Permite desacoplar el grafo estatico del arbol de exploracion dinamico.

```csharp
public class FlightSearchState
{
    public AirportNode Node { get; }                     // Referencia al aeropuerto actual
    public FlightSearchState? Parent { get; }            // Puntero al antecesor (para reconstruir la ruta)
    public double HeuristicDistance { get; }             // h(n): Estimacion al objetivo
    public double RealCumulativeDistance { get; }        // g(n): Distancia real acumulada (estadistica)

    public FlightSearchState(AirportNode node, FlightSearchState? parent, double heuristicDistance, double realCumulativeDistance)
    {
        Node = node;
        Parent = parent;
        HeuristicDistance = heuristicDistance;
        RealCumulativeDistance = realCumulativeDistance;
    }
}
```

---

## 2. Implementacion del Motor de Busqueda en `MainForm.cs`

La logica del algoritmo esta distribuida en tres fases principales dentro de la clase `MainForm`:
1. `StartOrResumeSearch()`: Inicializacion de estructuras.
2. `PerformOneHop()`: Ciclo de extraccion voraz y expansion.
3. `ReconstructPath()`: Trazado inverso del itinerario final.

---

### 2.1. Fase 1: Inicializacion (`StartOrResumeSearch`)

Prepara las estructuras de datos requeridas antes de comenzar las iteraciones:

```csharp
private void StartOrResumeSearch()
{
    if (startAirport == null || goalAirport == null) return;

    if (!isSearching)
    {
        ResetSearchOnly();

        // 1. Cola de prioridad tipada: Elemento = FlightSearchState, Prioridad = double (h(n))
        frontier = new PriorityQueue<FlightSearchState, double>();

        // 2. Tabla hash de visitados para busquedas y marcados en O(1)
        visited = new HashSet<AirportNode>();

        optimalPath.Clear();
        airportsEvaluated = 0;
        totalRealDistance = 0;

        // 3. Calculo de la heuristica del nodo inicial
        double initialH = startAirport.EstimateDistanceTo(goalAirport);

        // 4. Insercion en la frontera (el segundo parametro 'initialH' define la prioridad)
        frontier.Enqueue(new FlightSearchState(startAirport, null, initialH, 0), initialH);

        isSearching = true;
    }
}
```

#### Uso de `PriorityQueue<TElement, TPriority>`:
Se aprovecha la estructura nativa introducida en .NET 6+ (`PriorityQueue`), la cual implementa internamente un monticulo binario (min-heap).
- La operacion de encolado (`Enqueue`) tiene costo O(log N).
- La operacion de extraccion del minimo (`Dequeue`) tiene costo O(log N).
- La clave de prioridad es estrictamente el valor double de $h(n)$.

---

### 2.2. Fase 2: Ciclo de Seleccion Voraz y Expansion (`PerformOneHop`)

Este metodo ejecuta una iteracion individual del algoritmo (invocado tanto por el temporizador de animacion como por el boton de paso a paso):

```csharp
private void PerformOneHop()
{
    if (frontier == null || goalAirport == null) return;

    // Condicion de fallo: Si la cola se vacia sin alcanzar la meta
    if (frontier.Count == 0)
    {
        searchTimer.Stop();
        isSearching = false;
        MessageBox.Show("No se encontro ruta hacia el destino.");
        return;
    }

    // PASO A: Extraccion Voraz (Siempre obtiene el nodo con menor h(n))
    var currentState = frontier.Dequeue();
    var currentNode = currentState.Node;

    // PASO B: Control de nodos repetidos
    // Un mismo aeropuerto puede haber ingresado varias veces por rutas distintas
    if (visited.Contains(currentNode)) return;

    // PASO C: Marcar nodo como visitado
    visited.Add(currentNode);
    airportsEvaluated++;

    // PASO D: Prueba de Meta (Goal Test)
    if (currentNode == goalAirport)
    {
        searchTimer.Stop();
        isSearching = false;
        totalRealDistance = currentState.RealCumulativeDistance;
        
        // Reconstruir el camino desde el nodo meta
        ReconstructPath(currentState);
        UpdateStats();
        canvas.Invalidate();
        return;
    }

    // PASO E: Expansion de Vecinos (Generacion de Sucesores)
    foreach (var edge in currentNode.Neighbors)
    {
        var neighbor = edge.Key;
        double edgeDist = edge.Value;

        // Solo se evaluan vecinos no explorados previamente
        if (!visited.Contains(neighbor))
        {
            // Calculo voraz puro: solo se calcula h hacia la meta
            double h = neighbor.EstimateDistanceTo(goalAirport);
            double g = currentState.RealCumulativeDistance + edgeDist;

            // Se vincula 'currentState' como padre del nuevo nodo
            var childState = new FlightSearchState(neighbor, currentState, h, g);

            // Se inserta en el min-heap ordenado EXCLUSIVAMENTE por 'h'
            frontier.Enqueue(childState, h);
        }
    }

    UpdateStats();
    canvas.Invalidate();
}
```

---

### 2.3. Fase 3: Reconstruccion del Camino (`ReconstructPath`)

Una vez que `currentNode == goalAirport`, el puntero `Parent` permite retroceder a lo largo de toda la cadena de decisiones:

```csharp
private void ReconstructPath(FlightSearchState state)
{
    optimalPath.Clear();
    var curr = state;

    // Recorrido hacia atras desde la meta hasta la raiz (cuyo Parent es null)
    while (curr != null)
    {
        optimalPath.Add(curr.Node);
        curr = curr.Parent;
    }

    // Se invierte la lista para presentar el orden cronologico de salida a llegada
    optimalPath.Reverse();
}
```

---

## 3. Resumen de Complejidad en las Estructuras de Codigo

| Operacion en Codigo | Estructura Utilizada | Complejidad Temporal |
| :--- | :--- | :--- |
| Encolar sucesor `frontier.Enqueue(child, h)` | Min-Heap (`PriorityQueue`) | $O(\log N)$ |
| Extraer mejor nodo `frontier.Dequeue()` | Min-Heap (`PriorityQueue`) | $O(\log N)$ |
| Verificar visitado `visited.Contains(node)` | Tabla Hash (`HashSet`) | $O(1)$ |
| Iterar adyacentes `currentNode.Neighbors` | Diccionario (`Dictionary`) | $O(k)$ donde $k$ es el grado del nodo |
| Reconstruir camino `ReconstructPath(state)` | Lista enlazada simple | $O(d)$ donde $d$ es la profundidad del camino |

---

## 4. Comparativa de Codigo: Greedy Best-First vs A*

Para convertir este motor GBFS en un algoritmo A*, el unico cambio necesario en el codigo residiria en la prioridad con la que se inserta en la cola:

```csharp
// EN GREEDY BEST-FIRST SEARCH (Implementacion actual):
// Prioridad = h (solo heuristica)
frontier.Enqueue(childState, h);

// EN A* SEARCH:
// Prioridad = g + h (costo acumulado real + heuristica)
double f = g + h;
frontier.Enqueue(childState, f);
```

Esta linea evidencia por que GBFS es un algoritmo voraz: al omitir la variable `g` en la clave del monticulo, la cola no penaliza las rutas largas o ineficientes, premiando unicamente a los nodos que aparentan estar mas cerca del destino en linea recta.
