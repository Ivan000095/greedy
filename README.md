# Enrutamiento de Red mediante Greedy Best-First Search (GBFS)

Este proyecto implementa el algoritmo de inteligencia artificial Greedy Best-First Search (Búsqueda Primero el Mejor) en C# utilizando .NET 8 y Windows Forms. La aplicación permite simular de forma visual e interactiva el enrutamiento de paquetes en una red de telecomunicaciones.

---

## 1. Descripcion del Algoritmo

El algoritmo Greedy Best-First Search es un algoritmo de busqueda informada (heuristica) que explora un grafo expandiendo siempre el nodo que parece estar mas proximo al estado objetivo.

A diferencia de algoritmos de busqueda no informada (como BFS o DFS) que no poseen nocion de la direccion hacia la meta, GBFS utiliza una funcion heuristica de evaluacion:

    f(n) = h(n)

Donde:
- n: Representa el nodo actual a evaluar.
- h(n): Estima el costo restante desde el nodo n hasta el nodo objetivo.
- f(n): Es la funcion de prioridad usada para ordenar la frontera de busqueda.

### Caracteristicas Principales
- Naturaleza Voraz (Greedy): En cada decision de expansion, el algoritmo elige localmente la alternativa mas prometedora segun h(n), sin considerar los costos reales acumulados desde el origen (a diferencia de A*, que incluye g(n)).
- Rapidez de convergencia: Tiende a llegar rapidamente a la meta si la heuristica es adecuada.
- No optimalidad: No garantiza encontrar el camino de menor costo total, ya que puede ser inducido a rutas con aristas de alto costo real si estas apuntan visualmente o geometricamente hacia la meta.
- Estructura de datos: Utiliza una cola de prioridad (PriorityQueue en .NET) indexada por el valor de h(n).

---

## 2. Aplicacion al Enrutamiento de Paquetes en Red

En lugar de un laberinto convencional en cuadricula, este proyecto aplica el algoritmo a una topologia de red de conmutacion de paquetes (routers y enlaces de fibra optica):

### Componentes del Modelo
1. Nodos (Routers): Puntos de retransmision con coordenadas geograficas bidimensionales (X, Y).
2. Aristas (Enlaces de Transmision): Conexiones fisicas bidireccionales con una latencia real de propagacion medida en milisegundos (ms).
3. Nodo Origen (Start): Router emisor del paquete de datos.
4. Nodo Destino (Goal): Router receptor del paquete.

### Definicion de la Heuristica h(n)
La heuristica h(n) representa la latencia estimada minima en linea recta hacia el router destino, calculada a partir de la distancia euclidiana:

    distancia = sqrt((X_n - X_meta)^2 + (Y_n - Y_meta)^2)
    h(n) = distancia * 0.05 ms

Esta relacion modela el limite fisico de propagacion de pulsos luminosos en fibra optica (~5 ms por cada 100 unidades de distancia).

---

## 3. Funcionamiento Paso a Paso del Algoritmo

1. Inicializacion:
   - Se inserta el nodo origen en la cola de prioridad con prioridad h(origen).
   - Se inicializa el conjunto de nodos visitados como vacio.

2. Ciclo de Evaluacion:
   - Mientras la cola de prioridad no este vacia:
     1. Se extrae el nodo con el menor valor de h(n) de la cola de prioridad.
     2. Si ya ha sido procesado previamente, se descarta para evitar ciclos.
     3. Se marca el nodo como visitado.
     4. Si el nodo extraido coincide con el nodo destino, se detiene la busqueda exitosamente.
     5. En caso contrario, se inspeccionan todos sus enlaces adyacentes no visitados.
     6. Para cada vecino, se calcula h(vecino) hacia el destino y se encola en la frontera con dicha prioridad.

3. Reconstruccion del Camino:
   - Siguiendo los punteros de los nodos antecesores (Parent) desde el objetivo hasta el origen, se reconstruye la secuencia de saltos efectuada.

---

## 4. Requisitos del Sistema

- Sistema Operativo: Windows 10 / 11
- Entorno de Ejecucion: .NET SDK 8.0 o superior

---

## 5. Instrucciones de Compilacion y Ejecucion

Para compilar y ejecutar el proyecto localmente, abra una terminal en el directorio del proyecto y ejecute:

```bash
dotnet build
dotnet run
```

---

## 6. Funcionalidades de la Interfaz Grafica

- Agregar Router: Permite posicionar nuevos routers en cualquier punto del lienzo haciendo clic.
- Conectar Routers: Crea un enlace de red entre dos routers seleccionados calculando su latencia real.
- Definir Origen y Destino: Permite asignar que router actua como emisor (Start) y cual como receptor (Goal).
- Mover Router: Permite arrastrar routers con el cursor para reorganizar la topologia dinamicamente.
- Eliminar Router: Elimina routers y sus conexiones asociadas.
- Enrutamiento Animado: Ejecuta el algoritmo visualizando en tiempo real la exploracion de nodos y el camino seleccionado.
- Modo Paso a Paso: Permite avanzar un salto a la vez para estudiar cada decision de la cola de prioridad.
- Registro de Decisiones: Muestra en texto el historial de saltos y evaluaciones realizadas durante el proceso.
