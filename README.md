# Planificacion de Rutas Aereas mediante Greedy Best-First Search (GBFS)

Este proyecto implementa y simula el algoritmo Greedy Best-First Search (Busqueda Primero el Mejor) en C# con .NET 8 y Windows Forms, utilizando como modelo la navegacion aerea y planificacion de vuelos comerciales entre aeropuertos internacionales.

---

## 1. Que es Greedy Best-First Search

Greedy Best-First Search es un algoritmo de busqueda informada que tiene como proposito encontrar un camino entre un estado inicial y un estado objetivo dentro de un grafo o espacio de estados.

Su principio operativo fundamental es la toma de decisiones voraz (greedy): en cada paso, el algoritmo selecciona y expande exclusivamente el nodo que aparenta estar mas cerca del objetivo, guiandose unicamente por una funcion heuristica.

### Funcion de Evaluacion

A diferencia de algoritmos como A*, que combinan el costo acumulado desde el inicio g(n) con la heuristica h(n) mediante f(n) = g(n) + h(n), GBFS evalua los nodos empleando unicamente:

    f(n) = h(n)

Donde:
- n: Es el aeropuerto bajo evaluacion.
- h(n): Es el valor heuristico que estima la distancia en linea recta restante desde el aeropuerto n hasta el aeropuerto de destino.
- f(n): Es el valor de ordenamiento con el que se priorizan los nodos en la frontera de exploracion.

---

## 2. Aplicacion a la Navegacion y Rutas Aereas

El problema modela una aerolinea comercial planificando un vuelo con escalas:
- Nodos: Aeropuertos (Madrid, Londres, Paris, Frankfurt, Roma, Viena, Varsovia, Tokio, etc.) con codigos IATA y coordenadas geograficas.
- Aristas: Rutas aereas comerciales con distancia real de vuelo medida en kilometros (km).
- Estado Inicial: Aeropuerto de origen / salida (Start).
- Estado Objetivo: Aeropuerto de destino final / llegada (Goal).

### Funcion Heuristica h(n) en Aviacion
La heuristica representa la distancia ortodromica en linea recta (vuelo de pajaro) desde el aeropuerto n hasta el aeropuerto destino:

    delta_x = X_n - X_meta
    delta_y = Y_n - Y_meta
    h(n) = sqrt((delta_x)^2 + (delta_y)^2) * 10.0 km

En cada escala, el avion elige el siguiente aeropuerto que tenga la menor distancia geografica directa hacia el destino final, ignorando la distancia real de vuelo ya recorrida.

---

## 3. Estructuras de Datos Utilizadas

1. Frontera de Exploracion (Cola de Prioridad - PriorityQueue):
   Almacena los aeropuertos descubiertos pendientes de evaluacion, ordenados de menor a mayor segun su distancia heuristica h(n).

2. Conjunto de Nodos Visitados (HashSet):
   Registra los aeropuertos que ya han sido explorados para evitar escalas repetidas y bucles infinitos en el plan de vuelo.

3. Nodo de Busqueda (FlightSearchState):
   Almacena el aeropuerto actual, el puntero al aeropuerto de escala anterior (Parent), el valor h(n) y la distancia real acumulada g(n).

---

## 4. Descripcion Detallada del Algoritmo por Pasos

### Paso 1: Inicializacion
- Se valida que existan los aeropuertos de origen y destino.
- Se crea la cola de prioridad de frontera y el conjunto de visitados.
- Se calcula la distancia en linea recta inicial h(origen) hacia el destino.
- Se inserta el aeropuerto de origen como nodo raiz con prioridad h(origen).

### Paso 2: Verificacion de la Frontera
- Si la cola de prioridad esta vacia y no se ha alcanzado el destino, el algoritmo concluye indicando que no existe ruta aerea conectada (fracaso).
- Si contiene aeropuertos disponibles, se procede al Paso 3.

### Paso 3: Seleccion y Extraccion Voraz
- Se extrae de la cola de prioridad el aeropuerto con la menor distancia en linea recta h(n) al destino.
- Si ya fue visitado previamente, se descarta y se regresa al Paso 2.

### Paso 4: Marcado de Nodo
- Se agrega el aeropuerto extraido al conjunto de visitados.

### Paso 5: Comprobacion del Objetivo (Prueba de Meta)
- Si el aeropuerto coincide con el destino final, se concluye la busqueda y se procede al Paso 7.
- En caso contrario, se pasa al Paso 6.

### Paso 6: Expansion de Escalas Conectadas (Generacion de Sucesores)
- Se obtienen todas las rutas aereas que salen del aeropuerto actual hacia aeropuertos vecinos no visitados.
- Para cada aeropuerto adyacente:
  1. Se calcula su distancia en linea recta h(vecino) hacia el aeropuerto de destino.
  2. Se crea un nuevo estado de busqueda vinculando el aeropuerto actual como antecesor (Parent).
  3. Se inserta en la cola de prioridad con prioridad h(vecino).
- El algoritmo regresa al Paso 2.

### Paso 7: Reconstruccion del Plan de Vuelo
- Siguiendo los punteros Parent desde el aeropuerto de llegada hasta el de despegue inicial, se reconstruye la secuencia de escalas.
- Se invierte la lista para presentar el itinerario final: Origen -> Escala_1 -> Escala_2 -> ... -> Destino, reportando la distancia real acumulada en kilometros.

---

## 5. Pseudocodigo del Algoritmo

```
funcion PlanificarVueloGBFS(RedAerea, Origen, Destino):
    Frontera = nueva ColaDePrioridad()
    Visitados = nuevo Conjunto()

    h_inicial = DistanciaLineaRecta(Origen, Destino)
    EstadoInicial = nuevo FlightSearchState(Aeropuerto = Origen, Padre = nulo, H = h_inicial)
    Frontera.Insertar(EstadoInicial, Prioridad = h_inicial)

    mientras Frontera no este vacia:
        EstadoActual = Frontera.ExtraerMinimo()
        AeropuertoActual = EstadoActual.Aeropuerto

        si AeropuertoActual pertenece a Visitados:
            continuar al siguiente ciclo

        Visitados.Agregar(AeropuertoActual)

        si AeropuertoActual == Destino:
            retornar ReconstruirItinerario(EstadoActual)

        para cada Ruta en RedAerea.ObtenerRutas(AeropuertoActual):
            AeropuertoVecino = Ruta.Destino
            si AeropuertoVecino no pertenece a Visitados:
                h_vecino = DistanciaLineaRecta(AeropuertoVecino, Destino)
                NuevoEstado = nuevo FlightSearchState(Aeropuerto = AeropuertoVecino, Padre = EstadoActual, H = h_vecino)
                Frontera.Insertar(NuevoEstado, Prioridad = h_vecino)

    retornar Fracaso (No hay conexion aerea posible)
```

---

## 6. Analisis de Propiedades y Complejidad

- Complejidad Temporal: O(b^m) en el peor escenario, donde b es el factor de ramificacion (conexiones aereas promedio) y m la cantidad maxima de escalas. Con una heuristica euclidiana informativa, explora un numero minimo de escalas orientadas al destino.
- Complejidad Espacial: O(b^m), al retener en memoria los aeropuertos en la frontera de prioridad y el conjunto de visitados.
- Completitud: Es completo en grafos finitos con control de estados repetidos.
- Optimalidad: No es optimo. Puede seleccionar una ruta aerea directa orientada al destino pero con mayor kilometraje o consumo que una conexion con escala que requiera un desvio inicial.

---

## 7. Instrucciones de Compilacion y Ejecucion

### Requisitos
- Windows 10 / 11
- .NET SDK 8.0 o superior

### Ejecucion
```bash
dotnet build
dotnet run
```
