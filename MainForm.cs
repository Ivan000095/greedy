using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace GreedyBestFirstSearchGUI
{
    public partial class MainForm : Form
    {
        private readonly List<NetworkNode> nodes = new List<NetworkNode>();
        private NetworkNode? startRouter;
        private NetworkNode? goalRouter;

        // Búsqueda Greedy
        private PriorityQueue<PacketSearchState, double>? frontier;
        private HashSet<NetworkNode> visited = new HashSet<NetworkNode>();
        private List<NetworkNode> optimalPath = new List<NetworkNode>();
        private bool isSearching = false;
        private int packetsEvaluated = 0;
        private double totalRealLatency = 0;

        // Interacción visual
        private enum ToolMode { AddRouter, ConnectRouters, SelectStart, SelectGoal, MoveRouter, DeleteRouter }
        private ToolMode currentTool = ToolMode.AddRouter;
        private NetworkNode? connectionSource = null;
        private NetworkNode? draggedNode = null;
        private PointF dragOffset;

        // Controles UI
        private DoubleBufferedPanel canvas = null!;
        private TrackBar tbSpeed = null!;
        private Button btnStart = null!;
        private Button btnStep = null!;
        private Button btnReset = null!;
        private Button btnClear = null!;
        private Button btnPresetNet = null!;
        private Label lblStats = null!;
        private ListBox lbExecutionLog = null!;
        private System.Windows.Forms.Timer searchTimer = null!;

        // Radio buttons para herramientas
        private RadioButton rbAddRouter = null!;
        private RadioButton rbConnect = null!;
        private RadioButton rbStart = null!;
        private RadioButton rbGoal = null!;
        private RadioButton rbMove = null!;
        private RadioButton rbDelete = null!;

        private int routerCounter = 1;

        public MainForm()
        {
            InitializeComponentCustom();
            LoadDefaultNetwork();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Enrutamiento de Paquetes en Red - Greedy Best-First Search";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            searchTimer = new System.Windows.Forms.Timer();
            searchTimer.Interval = 100;
            searchTimer.Tick += SearchTimer_Tick;

            // Panel de Control Lateral con auto-scroll por si la pantalla tiene menor resolución
            var sidePanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 330,
                BackColor = Color.White,
                AutoScroll = true,
                Padding = new Padding(12)
            };

            int y = 10;

            var titleLbl = new Label
            {
                Text = "Enrutamiento de Red (GBFS)",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(10, y),
                AutoSize = true
            };
            sidePanel.Controls.Add(titleLbl);
            y += 26;

            var descLbl = new Label
            {
                Text = "Algoritmo voraz: en cada salto elige el router con menor latencia estimada al destino h(n).",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(90, 90, 90),
                Location = new Point(10, y),
                Size = new Size(295, 34)
            };
            sidePanel.Controls.Add(descLbl);
            y += 40;

            // Herramientas de Edición
            var gbTools = new GroupBox
            {
                Text = "Herramienta Dinámica",
                Location = new Point(10, y),
                Size = new Size(295, 175),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            sidePanel.Controls.Add(gbTools);

            rbAddRouter = new RadioButton { Text = "Agregar Router (Clic en mapa)", Location = new Point(12, 22), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.8f) };
            rbConnect = new RadioButton { Text = "Conectar Routers (Clic en A y B)", Location = new Point(12, 46), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbStart = new RadioButton { Text = "Definir Emisor / Origen (Start)", Location = new Point(12, 70), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbGoal = new RadioButton { Text = "Definir Receptor / Meta (Goal)", Location = new Point(12, 94), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbMove = new RadioButton { Text = "Mover Router (Arrastrar)", Location = new Point(12, 118), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbDelete = new RadioButton { Text = "Eliminar Router (Clic)", Location = new Point(12, 142), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };

            rbAddRouter.CheckedChanged += (s, e) => { if (rbAddRouter.Checked) SwitchTool(ToolMode.AddRouter); };
            rbConnect.CheckedChanged += (s, e) => { if (rbConnect.Checked) SwitchTool(ToolMode.ConnectRouters); };
            rbStart.CheckedChanged += (s, e) => { if (rbStart.Checked) SwitchTool(ToolMode.SelectStart); };
            rbGoal.CheckedChanged += (s, e) => { if (rbGoal.Checked) SwitchTool(ToolMode.SelectGoal); };
            rbMove.CheckedChanged += (s, e) => { if (rbMove.Checked) SwitchTool(ToolMode.MoveRouter); };
            rbDelete.CheckedChanged += (s, e) => { if (rbDelete.Checked) SwitchTool(ToolMode.DeleteRouter); };

            gbTools.Controls.AddRange(new Control[] { rbAddRouter, rbConnect, rbStart, rbGoal, rbMove, rbDelete });
            y += 182;

            // Velocidad
            var lblSpeed = new Label { Text = "Velocidad de Simulación:", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            sidePanel.Controls.Add(lblSpeed);
            y += 20;

            tbSpeed = new TrackBar { Location = new Point(15, y), Width = 280, Minimum = 1, Maximum = 100, Value = 40, TickFrequency = 10 };
            tbSpeed.ValueChanged += (s, e) => { searchTimer.Interval = Math.Max(10, 300 - (tbSpeed.Value * 3)); };
            sidePanel.Controls.Add(tbSpeed);
            y += 50; // Dejar espacio suficiente para no invadir el primer botón

            // Botones de Búsqueda
            btnStart = CreateButton("Enrutar Paquete (GBFS)", Color.FromArgb(40, 167, 69), 15, y);
            btnStart.Click += BtnStart_Click;
            sidePanel.Controls.Add(btnStart);
            y += 40;

            btnStep = CreateButton("Siguiente Salto (Paso)", Color.FromArgb(0, 123, 255), 15, y);
            btnStep.Click += BtnStep_Click;
            sidePanel.Controls.Add(btnStep);
            y += 40;

            btnReset = CreateButton("Reiniciar Búsqueda", Color.FromArgb(108, 117, 125), 15, y);
            btnReset.Click += (s, e) => ResetSearchOnly();
            sidePanel.Controls.Add(btnReset);
            y += 40;

            btnPresetNet = CreateButton("Cargar Red Predeterminada", Color.FromArgb(111, 66, 193), 15, y);
            btnPresetNet.Click += (s, e) => LoadDefaultNetwork();
            sidePanel.Controls.Add(btnPresetNet);
            y += 40;

            btnClear = CreateButton("Borrar Toda la Topología", Color.FromArgb(220, 53, 69), 15, y);
            btnClear.Click += (s, e) => ClearTopology();
            sidePanel.Controls.Add(btnClear);
            y += 45;

            // Panel de Resultados
            lblStats = new Label
            {
                Text = "Emisor: -\nReceptor: -\nRouters evaluados: 0\nSaltos: 0 | Latencia: 0.0 ms",
                Location = new Point(15, y),
                Size = new Size(280, 68),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8.5f),
                Padding = new Padding(5)
            };
            sidePanel.Controls.Add(lblStats);
            y += 75;

            var lblLogHeader = new Label { Text = "Registro de Saltos:", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            sidePanel.Controls.Add(lblLogHeader);
            y += 20;

            lbExecutionLog = new ListBox
            {
                Location = new Point(15, y),
                Size = new Size(280, 110),
                Font = new Font("Consolas", 8f),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            sidePanel.Controls.Add(lbExecutionLog);

            // Canvas de Red (Se agrega primero al Form para que Dock.Fill respete el área restante después del sidePanel)
            canvas = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(246, 248, 250)
            };

            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;

            this.Controls.Add(canvas);
            this.Controls.Add(sidePanel);
        }

        private Button CreateButton(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(280, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SwitchTool(ToolMode tool)
        {
            currentTool = tool;
            connectionSource = null;
            draggedNode = null;
            canvas.Invalidate();
        }

        private void LoadDefaultNetwork()
        {
            ResetSearchOnly();
            nodes.Clear();
            routerCounter = 1;

            // Topología de red WAN representativa y espaciada para evitar superposición
            var rMadrid = new NetworkNode("R1", "Madrid", 140, 260);
            var rParis = new NetworkNode("R2", "París", 340, 150);
            var rLondres = new NetworkNode("R3", "Londres", 280, 70);
            var rFrankfurt = new NetworkNode("R4", "Frankfurt", 480, 140);
            var rRoma = new NetworkNode("R5", "Roma", 360, 380);
            var rViena = new NetworkNode("R6", "Viena", 530, 260);
            var rVarsovia = new NetworkNode("R7", "Varsovia", 680, 140);
            var rAtenas = new NetworkNode("R8", "Atenas", 570, 430);
            var rEstambul = new NetworkNode("R9", "Estambul", 730, 360);
            var rTokio = new NetworkNode("R10", "Tokio", 830, 230); // Servidor destino

            // Enlaces de fibra con latencia real medida en milisegundos
            rMadrid.AddNeighbor(rParis, 18);
            rMadrid.AddNeighbor(rRoma, 25);
            rMadrid.AddNeighbor(rLondres, 22);

            rLondres.AddNeighbor(rParis, 9);
            rLondres.AddNeighbor(rFrankfurt, 15);

            rParis.AddNeighbor(rFrankfurt, 12);
            rParis.AddNeighbor(rRoma, 20);

            rRoma.AddNeighbor(rViena, 14);
            rRoma.AddNeighbor(rAtenas, 19);

            rFrankfurt.AddNeighbor(rViena, 10);
            rFrankfurt.AddNeighbor(rVarsovia, 16);

            rViena.AddNeighbor(rVarsovia, 11);
            rViena.AddNeighbor(rAtenas, 21);

            rVarsovia.AddNeighbor(rTokio, 75); // Enlace transcontinental norte

            rAtenas.AddNeighbor(rEstambul, 12);
            rEstambul.AddNeighbor(rTokio, 88);

            nodes.AddRange(new[] { rMadrid, rParis, rLondres, rFrankfurt, rRoma, rViena, rVarsovia, rAtenas, rEstambul, rTokio });
            routerCounter = 11;

            startRouter = rMadrid;
            goalRouter = rTokio;

            lbExecutionLog.Items.Clear();
            lbExecutionLog.Items.Add("Topología WAN cargada.");
            lbExecutionLog.Items.Add($"Emisor: {startRouter.Label} -> Receptor: {goalRouter.Label}");

            UpdateStats();
            canvas.Invalidate();
        }

        private void ClearTopology()
        {
            ResetSearchOnly();
            nodes.Clear();
            startRouter = null;
            goalRouter = null;
            connectionSource = null;
            routerCounter = 1;
            lbExecutionLog.Items.Clear();
            UpdateStats();
            canvas.Invalidate();
        }

        private void Canvas_MouseDown(object? sender, MouseEventArgs e)
        {
            var clickedNode = GetNodeAt(e.X, e.Y);

            switch (currentTool)
            {
                case ToolMode.AddRouter:
                    if (clickedNode == null)
                    {
                        var newRouter = new NetworkNode($"R{routerCounter}", $"Router-{routerCounter}", e.X, e.Y);
                        routerCounter++;
                        nodes.Add(newRouter);
                        if (startRouter == null) startRouter = newRouter;
                        else if (goalRouter == null) goalRouter = newRouter;
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;

                case ToolMode.ConnectRouters:
                    if (clickedNode != null)
                    {
                        if (connectionSource == null)
                        {
                            connectionSource = clickedNode;
                        }
                        else if (connectionSource != clickedNode)
                        {
                            // Calcular latencia aproximada por distancia de fibra
                            double latency = Math.Round(connectionSource.EstimateLatencyTo(clickedNode), 1);
                            connectionSource.AddNeighbor(clickedNode, latency);
                            lbExecutionLog.Items.Add($"Enlace: {connectionSource.Label} <-> {clickedNode.Label} ({latency} ms)");
                            connectionSource = null;
                            canvas.Invalidate();
                        }
                    }
                    break;

                case ToolMode.SelectStart:
                    if (clickedNode != null && clickedNode != goalRouter)
                    {
                        startRouter = clickedNode;
                        ResetSearchOnly();
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;

                case ToolMode.SelectGoal:
                    if (clickedNode != null && clickedNode != startRouter)
                    {
                        goalRouter = clickedNode;
                        ResetSearchOnly();
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;

                case ToolMode.MoveRouter:
                    if (clickedNode != null)
                    {
                        draggedNode = clickedNode;
                        dragOffset = new PointF(e.X - clickedNode.X, e.Y - clickedNode.Y);
                    }
                    break;

                case ToolMode.DeleteRouter:
                    if (clickedNode != null)
                    {
                        foreach (var other in nodes)
                        {
                            other.Neighbors.Remove(clickedNode);
                        }
                        nodes.Remove(clickedNode);
                        if (startRouter == clickedNode) startRouter = null;
                        if (goalRouter == clickedNode) goalRouter = null;
                        ResetSearchOnly();
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;
            }
        }

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (currentTool == ToolMode.MoveRouter && draggedNode != null)
            {
                draggedNode.X = e.X - dragOffset.X;
                draggedNode.Y = e.Y - dragOffset.Y;
                canvas.Invalidate();
            }
        }

        private void Canvas_MouseUp(object? sender, MouseEventArgs e)
        {
            draggedNode = null;
        }

        private NetworkNode? GetNodeAt(float x, float y)
        {
            const float radius = 24;
            return nodes.FirstOrDefault(n => Math.Sqrt(Math.Pow(n.X - x, 2) + Math.Pow(n.Y - y, 2)) <= radius);
        }

        private void Canvas_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Dibujar enlaces de red
            var drawnEdges = new HashSet<(NetworkNode, NetworkNode)>();
            foreach (var node in nodes)
            {
                foreach (var edge in node.Neighbors)
                {
                    var neighbor = edge.Key;
                    if (drawnEdges.Contains((neighbor, node))) continue;
                    drawnEdges.Add((node, neighbor));

                    bool isPathEdge = false;
                    for (int i = 0; i < optimalPath.Count - 1; i++)
                    {
                        if ((optimalPath[i] == node && optimalPath[i + 1] == neighbor) ||
                            (optimalPath[i] == neighbor && optimalPath[i + 1] == node))
                        {
                            isPathEdge = true;
                            break;
                        }
                    }

                    Color edgeColor = isPathEdge ? Color.FromArgb(139, 92, 246) : Color.FromArgb(203, 213, 225);
                    float edgeWidth = isPathEdge ? 4.5f : 2f;

                    using (var pen = new Pen(edgeColor, edgeWidth))
                    {
                        g.DrawLine(pen, node.X, node.Y, neighbor.X, neighbor.Y);
                    }

                    // Etiqueta de latencia en el centro de la línea
                    float midX = (node.X + neighbor.X) / 2;
                    float midY = (node.Y + neighbor.Y) / 2;

                    string latText = $"{edge.Value} ms";
                    using (var font = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                    {
                        var size = g.MeasureString(latText, font);
                        var badgeRect = new RectangleF(midX - size.Width / 2 - 2, midY - size.Height / 2 - 1, size.Width + 4, size.Height + 2);
                        g.FillRectangle(Brushes.White, badgeRect);
                        g.DrawRectangle(Pens.LightGray, badgeRect.X, badgeRect.Y, badgeRect.Width, badgeRect.Height);
                        g.DrawString(latText, font, Brushes.Black, midX - size.Width / 2, midY - size.Height / 2);
                    }
                }
            }

            // Si se está conectando un router, dibujar línea temporal
            if (currentTool == ToolMode.ConnectRouters && connectionSource != null)
            {
                using (var dashedPen = new Pen(Color.FromArgb(0, 123, 255), 2f) { DashStyle = DashStyle.Dash })
                {
                    var mousePos = canvas.PointToClient(Cursor.Position);
                    g.DrawLine(dashedPen, connectionSource.X, connectionSource.Y, mousePos.X, mousePos.Y);
                }
            }

            // 2. Dibujar Routers (Nodos)
            const float radius = 24;
            foreach (var node in nodes)
            {
                Color fillColor;
                Color borderColor = Color.FromArgb(70, 70, 70);

                if (node == startRouter)
                {
                    fillColor = Color.FromArgb(40, 167, 69); // Verde: Emisor
                    borderColor = Color.FromArgb(25, 105, 44);
                }
                else if (node == goalRouter)
                {
                    fillColor = Color.FromArgb(220, 53, 69); // Rojo: Receptor
                    borderColor = Color.FromArgb(145, 30, 40);
                }
                else if (optimalPath.Contains(node))
                {
                    fillColor = Color.FromArgb(111, 66, 193); // Púrpura: Camino final
                    borderColor = Color.FromArgb(75, 40, 135);
                }
                else if (visited.Contains(node))
                {
                    fillColor = Color.FromArgb(150, 200, 245); // Azul suave: Inspeccionado
                    borderColor = Color.FromArgb(0, 100, 200);
                }
                else if (frontier != null && frontier.UnorderedItems.Any(item => item.Element.Node == node))
                {
                    fillColor = Color.FromArgb(255, 193, 7); // Amarillo: En cola de prioridad por h(n)
                    borderColor = Color.FromArgb(180, 130, 0);
                }
                else
                {
                    fillColor = Color.White;
                }

                if (node == connectionSource)
                {
                    borderColor = Color.FromArgb(0, 123, 255);
                }

                // Círculo del router
                var rect = new RectangleF(node.X - radius, node.Y - radius, radius * 2, radius * 2);
                using (var brush = new SolidBrush(fillColor))
                using (var pen = new Pen(borderColor, 2.5f))
                {
                    g.FillEllipse(brush, rect);
                    g.DrawEllipse(pen, rect);
                }

                // ID dentro del círculo si es nodo inicial o meta
                if (node == startRouter)
                {
                    TextRenderer.DrawText(g, "START", new Font("Segoe UI", 7.5f, FontStyle.Bold), Rectangle.Round(rect), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else if (node == goalRouter)
                {
                    TextRenderer.DrawText(g, "GOAL", new Font("Segoe UI", 7.5f, FontStyle.Bold), Rectangle.Round(rect), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Nombre/Etiqueta abajo del nodo con caja de fondo limpio para evitar cruce con cables
                string label = node.Label;
                using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                {
                    var size = g.MeasureString(label, font);
                    float labelX = node.X - size.Width / 2;
                    float labelY = node.Y + radius + 4;
                    var labelBg = new RectangleF(labelX - 3, labelY - 1, size.Width + 6, size.Height + 2);
                    g.FillRectangle(Brushes.White, labelBg);
                    g.DrawRectangle(Pens.LightGray, labelBg.X, labelBg.Y, labelBg.Width, labelBg.Height);
                    g.DrawString(label, font, Brushes.Black, labelX, labelY);
                }

                // Valor de heurística h(n) encima del nodo con recuadro ordenado
                if (goalRouter != null && node != goalRouter)
                {
                    double h = node.EstimateLatencyTo(goalRouter);
                    string hText = $"h: {h:F1}ms";
                    using (var hFont = new Font("Consolas", 8f, FontStyle.Regular))
                    {
                        var size = g.MeasureString(hText, hFont);
                        float hX = node.X - size.Width / 2;
                        float hY = node.Y - radius - size.Height - 4;
                        var hBg = new RectangleF(hX - 3, hY - 1, size.Width + 6, size.Height + 2);
                        g.FillRectangle(Brushes.White, hBg);
                        g.DrawRectangle(Pens.Silver, hBg.X, hBg.Y, hBg.Width, hBg.Height);
                        g.DrawString(hText, hFont, Brushes.DarkSlateGray, hX, hY);
                    }
                }
            }

            // 3. Leyenda en la esquina superior derecha
            DrawLegend(g);
        }

        private void DrawLegend(Graphics g)
        {
            int lx = canvas.Width - 195;
            int ly = 10;
            if (lx < 10) return;

            var legendRect = new Rectangle(lx, ly, 185, 115);
            g.FillRectangle(Brushes.White, legendRect);
            g.DrawRectangle(Pens.Gray, legendRect);

            using (var boldFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var regFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            {
                g.DrawString("Leyenda de Estados:", boldFont, Brushes.Black, lx + 8, ly + 6);

                DrawLegendItem(g, lx + 10, ly + 26, Color.FromArgb(40, 167, 69), "Origen (Start)", regFont);
                DrawLegendItem(g, lx + 10, ly + 44, Color.FromArgb(220, 53, 69), "Destino (Goal)", regFont);
                DrawLegendItem(g, lx + 10, ly + 62, Color.FromArgb(255, 193, 7), "Frontera h(n)", regFont);
                DrawLegendItem(g, lx + 10, ly + 80, Color.FromArgb(150, 200, 245), "Explorado", regFont);
                DrawLegendItem(g, lx + 10, ly + 98, Color.FromArgb(111, 66, 193), "Camino voraz", regFont);
            }
        }

        private void DrawLegendItem(Graphics g, int x, int y, Color color, string text, Font font)
        {
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, x, y + 2, 12, 10);
                g.DrawRectangle(Pens.Black, x, y + 2, 12, 10);
            }
            g.DrawString(text, font, Brushes.Black, x + 18, y);
        }

        private void StartOrResumeSearch()
        {
            if (startRouter == null || goalRouter == null)
            {
                MessageBox.Show("Debes seleccionar un router Emisor (Start) y un Receptor (Goal).", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isSearching)
            {
                ResetSearchOnly();

                frontier = new PriorityQueue<PacketSearchState, double>();
                visited = new HashSet<NetworkNode>();
                optimalPath.Clear();
                packetsEvaluated = 0;
                totalRealLatency = 0;

                double initialH = startRouter.EstimateLatencyTo(goalRouter);
                frontier.Enqueue(new PacketSearchState(startRouter, null, initialH, 0), initialH);

                lbExecutionLog.Items.Clear();
                lbExecutionLog.Items.Add($"Iniciando GBFS: {startRouter.Label} -> {goalRouter.Label}");
                lbExecutionLog.Items.Add($"Heurística inicial h = {initialH} ms");

                isSearching = true;
            }
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (searchTimer.Enabled)
            {
                searchTimer.Stop();
                btnStart.Text = "▶ Reanudar Enrutamiento";
                btnStart.BackColor = Color.FromArgb(16, 185, 129);
            }
            else
            {
                StartOrResumeSearch();
                searchTimer.Interval = Math.Max(10, 300 - (tbSpeed.Value * 3));
                searchTimer.Start();
                btnStart.Text = "⏸ Pausar";
                btnStart.BackColor = Color.FromArgb(245, 158, 11);
            }
        }

        private void BtnStep_Click(object? sender, EventArgs e)
        {
            if (searchTimer.Enabled)
            {
                searchTimer.Stop();
                btnStart.Text = "▶ Reanudar Enrutamiento";
                btnStart.BackColor = Color.FromArgb(16, 185, 129);
            }

            StartOrResumeSearch();
            PerformOneHop();
        }

        private void SearchTimer_Tick(object? sender, EventArgs e)
        {
            PerformOneHop();
        }

        private void PerformOneHop()
        {
            if (frontier == null || goalRouter == null) return;

            if (frontier.Count == 0)
            {
                searchTimer.Stop();
                isSearching = false;
                btnStart.Text = "▶ Enrutar Paquete (GBFS)";
                btnStart.BackColor = Color.FromArgb(16, 185, 129);
                lbExecutionLog.Items.Add("X No hay enlace disponible hacia el destino.");
                MessageBox.Show("No se encontró ruta de red hacia el receptor.", "Destino inalcanzable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentState = frontier.Dequeue();
            var currentNode = currentState.Node;

            if (visited.Contains(currentNode)) return;

            visited.Add(currentNode);
            packetsEvaluated++;

            lbExecutionLog.Items.Add($"-> Salto a '{currentNode.Label}' | h={currentState.HeuristicLatency:F1}ms");
            lbExecutionLog.TopIndex = lbExecutionLog.Items.Count - 1;

            // ¿Llegamos al servidor receptor?
            if (currentNode == goalRouter)
            {
                searchTimer.Stop();
                isSearching = false;
                btnStart.Text = "▶ Enrutar Paquete (GBFS)";
                btnStart.BackColor = Color.FromArgb(16, 185, 129);

                totalRealLatency = currentState.RealCumulativeLatency;
                ReconstructPath(currentState);

                lbExecutionLog.Items.Add($"✓ ¡Paquete entregado con éxito!");
                lbExecutionLog.Items.Add($"Latencia total: {totalRealLatency:F1} ms");
                UpdateStats();
                canvas.Invalidate();
                return;
            }

            // Evaluar los routers vecinos conectados
            foreach (var edge in currentNode.Neighbors)
            {
                var neighbor = edge.Key;
                double edgeLatency = edge.Value;

                if (!visited.Contains(neighbor))
                {
                    double h = neighbor.EstimateLatencyTo(goalRouter); // Heurística Greedy pura
                    double g = currentState.RealCumulativeLatency + edgeLatency;

                    var childState = new PacketSearchState(neighbor, currentState, h, g);
                    frontier.Enqueue(childState, h); // Prioridad absoluta por menor h(n)
                }
            }

            UpdateStats();
            canvas.Invalidate();
        }

        private void ReconstructPath(PacketSearchState state)
        {
            optimalPath.Clear();
            var curr = state;
            while (curr != null)
            {
                optimalPath.Add(curr.Node);
                curr = curr.Parent;
            }
            optimalPath.Reverse();
        }

        private void ResetSearchOnly()
        {
            searchTimer.Stop();
            isSearching = false;
            btnStart.Text = "▶ Enrutar Paquete (GBFS)";
            btnStart.BackColor = Color.FromArgb(16, 185, 129);

            frontier = null;
            visited.Clear();
            optimalPath.Clear();
            packetsEvaluated = 0;
            totalRealLatency = 0;

            UpdateStats();
            canvas.Invalidate();
        }

        private void UpdateStats()
        {
            string startStr = startRouter != null ? startRouter.Label : "No definido";
            string goalStr = goalRouter != null ? goalRouter.Label : "No definido";
            int pathHops = optimalPath.Count > 0 ? optimalPath.Count - 1 : 0;

            lblStats.Text = $"Emisor: {startStr}\nReceptor: {goalStr}\nRouters evaluados: {packetsEvaluated}\nSaltos: {pathHops} | Latencia: {totalRealLatency:F1} ms";
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}
