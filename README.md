# Algoritmo Greedy Best-First Search (GBFS)

Este proyecto implementa y simula el algoritmo Greedy Best-First Search (Busqueda Primero el Mejor) en C# con .NET 8 y Windows Forms, utilizando como modelo el enrutamiento de paquetes en una red de telecomunicaciones.

---

## 1. Que es Greedy Best-First Search

Greedy Best-First Search es un algoritmo de busqueda informada que tiene como proposito encontrar un camino entre un estado inicial y un estado objetivo dentro de un grafo o espacio de estados.

Su principio operativo fundamental es la toma de decisiones voraz (greedy): en cada paso, el algoritmo selecciona y expande exclusivamente el nodo que aparenta estar mas cerca del objetivo, guiandose unicamente por una funcion heuristica.

### Funcion de Evaluacion

A diferencia de algoritmos como A*, que combinan el costo acumulado desde el inicio g(n) con la heuristica h(n) mediante f(n) = g(n) + h(n), GBFS evalua los nodos empleando unicamente:

    f(n) = h(n)

Donde:
- n: Es el nodo bajo evaluacion.
- h(n): Es el valor heuristico que estima la distancia o costo restante desde el nodo n hasta el objetivo.
- f(n): Es el valor de ordenamiento con el que se priorizan los nodos en la frontera de exploracion.

---

## 2. Estructuras de Datos Utilizadas

Para implementar el algoritmo de manera eficiente se emplean las siguientes estructuras:

1. Frontera de Exploracion (Cola de Prioridad - PriorityQueue):
   Almacena los nodos descubiertos que aun no han sido expandidos. La cola esta ordenada de menor a mayor segun el valor de h(n), de tal forma que la operacion de extraccion siempre obtiene el nodo con menor valor heuristico en tiempo O(log N).

2. Conjunto de Nodos Visitados (HashSet):
   Registra los identificadores de los nodos que ya han sido extraidos y explorados. Su funcion es evitar ciclos infinitos y prevenir el reprocesamiento innecesario de nodos en grafos con bucles.

3. Nodo de Busqueda (SearchNode):
   Estructura auxiliar que contiene:
   - Referencia al nodo actual del grafo.
   - Puntero al nodo padre (Parent), indispensable para reconstruir el camino una vez alcanzado el destino.
   - Valor heuristico h(n).
   - Costo acumulado real (utilizado para metricas informativas).

---

## 3. Descripcion Detallada del Algoritmo por Pasos

El algoritmo opera siguiendo una secuencia estricta de pasos:

### Paso 1: Inicializacion
- Se valida que tanto el nodo origen (Start) como el nodo destino (Goal) existan en el grafo.
- Se crea la cola de prioridad vacia para la frontera de busqueda.
- Se crea el conjunto de nodos visitados vacio.
- Se calcula la heuristica inicial h(origen) con respecto al nodo destino.
- Se empaqueta el nodo origen como el nodo raiz (con padre igual a nulo) y se inserta en la cola de prioridad con prioridad h(origen).

### Paso 2: Verificacion de la Frontera
- Se evalua si la cola de prioridad contiene elementos:
  - Si la cola esta vacia y no se ha alcanzado la meta, el algoritmo concluye indicando que no existe ningun camino posible entre el origen y el destino (fracaso).
  - Si la cola contiene al menos un elemento, se procede al Paso 3.

### Paso 3: Seleccion y Extraccion Voraz
- Se extrae de la cola de prioridad el elemento con el menor valor de h(n).
- Si el nodo extraido ya pertenece al conjunto de visitados (situacion que ocurre cuando un mismo nodo fue insertado por multiples caminos con distintas prioridades), se descarta inmediatamente y se regresa al Paso 2.

### Paso 4: Marcado de Nodo
- El nodo extraido se agrega al conjunto de visitados para asegurar que no sea procesado nuevamente en futuras iteraciones.

### Paso 5: Comprobacion del Objetivo (Prueba de Meta)
- Se compara el nodo extraido con el nodo destino:
  - Si coinciden, el objetivo ha sido alcanzado. Se detiene el ciclo de busqueda y se transfiere el control al Paso 7 (Reconstruccion del Camino).
  - Si no coinciden, se procede al Paso 6 para generar los sucesores.

### Paso 6: Expansion de Nodos Vecinos (Generacion de Sucesores)
- Se obtienen todos los nodos adyacentes conectados por aristas al nodo actual.
- Para cada nodo vecino:
  1. Se verifica si el vecino ya se encuentra en el conjunto de nodos visitados. Si ya fue visitado, se ignora.
  2. Si el vecino no ha sido visitado:
     - Se calcula su valor heuristico h(vecino) hacia el nodo destino.
     - Se crea un nuevo SearchNode para este vecino, estableciendo como nodo padre al nodo actual extraido en el Paso 3.
     - Se inserta en la cola de prioridad utilizando su valor de h(vecino) como clave de ordenamiento.
- Finalizada la evaluacion de todos los vecinos, el algoritmo regresa al Paso 2 para continuar con la siguiente iteracion.

### Paso 7: Reconstruccion del Camino
- A partir del nodo de busqueda que alcanzo el objetivo:
  1. Se registra el nodo en una lista de ruta.
  2. Se sigue el puntero hacia su nodo padre.
  3. Se repite este proceso sucesivamente hasta alcanzar el nodo origen (cuyo padre es nulo).
  4. Se invierte la lista resultante para obtener la secuencia ordenada desde el origen hasta el destino: Origen -> Nodo_1 -> Nodo_2 -> ... -> Destino.
- Se calcula la suma de los costos reales de las aristas recorridas para reportar la metrica de costo total.

---

## 4. Pseudocodigo del Algoritmo

A continuacion se presenta la representacion formal del flujo algoritmico:

```
funcion GreedyBestFirstSearch(Grafo, Origen, Destino):
    Frontera = nueva ColaDePrioridad()
    Visitados = nuevo Conjunto()

    h_inicial = Heuristica(Origen, Destino)
    NodoInicial = nuevo SearchNode(Estado = Origen, Padre = nulo, H = h_inicial)
    Frontera.Insertar(NodoInicial, Prioridad = h_inicial)

    mientras Frontera no este vacia:
        NodoActual = Frontera.ExtraerMinimo()
        EstadoActual = NodoActual.Estado

        si EstadoActual pertenece a Visitados:
            continuar al siguiente ciclo

        Visitados.Agregar(EstadoActual)

        si EstadoActual == Destino:
            retornar ReconstruirCamino(NodoActual)

        para cada Arista en Grafo.ObtenerAdyacentes(EstadoActual):
            Vecino = Arista.Destino
            si Vecino no pertenece a Visitados:
                h_vecino = Heuristica(Vecino, Destino)
                NodoHijo = nuevo SearchNode(Estado = Vecino, Padre = NodoActual, H = h_vecino)
                Frontera.Insertar(NodoHijo, Prioridad = h_vecino)

    retornar Fracaso (No hay camino)
```

---

## 5. Analisis de Propiedades y Complejidad

- Complejidad Temporal:
  En el peor de los casos, la complejidad temporal es O(b^m), donde b es el factor de ramificacion (numero promedio de sucesores por nodo) y m es la profundidad maxima del espacio de busqueda. Sin embargo, con una funcion heuristica precisa, el algoritmo reduce drasticamente los nodos explorados en comparacion con busquedas a ciegas.

- Complejidad Espacial:
  Es O(b^m), ya que todos los nodos generados permanecen almacenados en la cola de prioridad de la frontera o en el conjunto de visitados.

- Completitud:
  El algoritmo es completo en espacios de estados finitos siempre que se emplee deteccion de estados repetidos (conjunto de visitados), impidiendo bucles infinitos.

- Optimalidad:
  El algoritmo no es optimo. Dado que no toma en cuenta el costo acumulado de las aristas g(n), puede seleccionar una arista que parece orientada hacia el destino pero cuyo costo real de transporte sea considerablemente mas elevado que una alternativa que requiera un desvio inicial.

---

## 6. Modelo de Heuristica Aplicado al Enrutamiento de Red

En la aplicacion grafica, el grafo representa una red de routers interconectados por cables de fibra optica con latencia medida en milisegundos (ms).

La heuristica h(n) calcula la distancia euclidiana en linea recta desde el router n (X_n, Y_n) hasta el router objetivo (X_meta, Y_meta):

    delta_x = X_n - X_meta
    delta_y = Y_n - Y_meta
    distancia = sqrt((delta_x)^2 + (delta_y)^2)
    h(n) = distancia * 0.05 ms

El factor 0.05 ms modela el retardo fisico aproximado de propagacion de la luz en un medio de fibra optica (~5 ms por cada 100 unidades de distancia).

---

## 7. Instrucciones de Compilacion y Ejecucion

### Requisitos
- Windows 10 / 11
- .NET SDK 8.0 o superior

### Ejecucion
Abra una consola en el directorio raiz del proyecto y ejecute:

```bash
dotnet build
dotnet run
```
