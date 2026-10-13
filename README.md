# Simulador de Enrutamiento de Red con Greedy Best-First Search (GBFS)

Este proyecto implementa el algoritmo **Greedy Best-First Search (Búsqueda Primero el Mejor)** en C# con interfaz gráfica interactiva (**Windows Forms**, .NET 8).

---

## 🎯 Descripción del Problema
En lugar de un laberinto convencional, la aplicación modela el **enrutamiento de paquetes IP en redes WAN / fibra óptica**:
- **Nodos (Routers)**: Puntos de red con coordenadas espaciales.
- **Aristas (Enlaces)**: Cables con **latencia real medida en milisegundos (ms)**.
- **Función Heurística $h(n)$**: Estimación de latencia física en línea recta hacia el servidor de destino ($\approx 5\text{ ms} / 100\text{ km}$).
- **Comportamiento Voraz (Greedy)**: En cada salto, el paquete avanza hacia el router con menor valor de $h(n)$, mostrando la naturaleza del algoritmo.

---

## 🚀 Funcionalidades Dinámicas
- **Edición en tiempo real**: Agregar routers, conectar cables con latencias calculadas, mover nodos o eliminarlos.
- **Configuración flexible**: Definir emisor (*Start*) y receptor (*Goal*).
- **Control de simulación**:
  - Enrutamiento animado con control de velocidad.
  - Modo **Paso a Paso** para inspeccionar cada salto.
  - Registro de decisiones (*Hop Decisions*) en vivo.
- **Leyenda de colores**:
  - 🟢 **Origen (Start)**
  - 🔴 **Destino (Goal)**
  - 🟡 **Frontera de prioridad**
  - 🔵 **Routers explorados**
  - 🟣 **Camino voraz final**

---

## 🛠 Requisitos y Ejecución

- **SDK**: .NET 8.0 o superior
- **SO**: Windows

```bash
dotnet run
```
