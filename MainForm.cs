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
        private readonly List<AirportNode> nodes = new List<AirportNode>();
        private AirportNode? startAirport;
        private AirportNode? goalAirport;

        // Búsqueda Greedy
        private PriorityQueue<FlightSearchState, double>? frontier;
        private HashSet<AirportNode> visited = new HashSet<AirportNode>();
        private List<AirportNode> optimalPath = new List<AirportNode>();
        private bool isSearching = false;
        private int airportsEvaluated = 0;
        private double totalRealDistance = 0;

        // Interacción visual
        private enum ToolMode { AddAirport, ConnectAirports, SelectStart, SelectGoal, MoveAirport, DeleteAirport }
        private ToolMode currentTool = ToolMode.AddAirport;
        private AirportNode? connectionSource = null;
        private AirportNode? draggedNode = null;
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
        private RadioButton rbAddAirport = null!;
        private RadioButton rbConnect = null!;
        private RadioButton rbStart = null!;
        private RadioButton rbGoal = null!;
        private RadioButton rbMove = null!;
        private RadioButton rbDelete = null!;

        private int airportCounter = 1;

        public MainForm()
        {
            InitializeComponentCustom();
            LoadDefaultNetwork();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Navegación y Planificación de Vuelos - Greedy Best-First Search";
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
                Text = "Rutas de Vuelo (GBFS)",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(10, y),
                AutoSize = true
            };
            sidePanel.Controls.Add(titleLbl);
            y += 26;

            var descLbl = new Label
            {
                Text = "Algoritmo voraz: en cada escala elige el aeropuerto con menor distancia en línea recta al destino h(n).",
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

            rbAddAirport = new RadioButton { Text = "Agregar Aeropuerto (Clic en mapa)", Location = new Point(12, 22), AutoSize = true, Checked = true, Font = new Font("Segoe UI", 8.8f) };
            rbConnect = new RadioButton { Text = "Crear Ruta Aérea (Clic en A y B)", Location = new Point(12, 46), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbStart = new RadioButton { Text = "Aeropuerto de Origen (Salida)", Location = new Point(12, 70), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbGoal = new RadioButton { Text = "Aeropuerto de Destino (Llegada)", Location = new Point(12, 94), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbMove = new RadioButton { Text = "Mover Aeropuerto (Arrastrar)", Location = new Point(12, 118), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };
            rbDelete = new RadioButton { Text = "Eliminar Aeropuerto (Clic)", Location = new Point(12, 142), AutoSize = true, Font = new Font("Segoe UI", 8.8f) };

            rbAddAirport.CheckedChanged += (s, e) => { if (rbAddAirport.Checked) SwitchTool(ToolMode.AddAirport); };
            rbConnect.CheckedChanged += (s, e) => { if (rbConnect.Checked) SwitchTool(ToolMode.ConnectAirports); };
            rbStart.CheckedChanged += (s, e) => { if (rbStart.Checked) SwitchTool(ToolMode.SelectStart); };
            rbGoal.CheckedChanged += (s, e) => { if (rbGoal.Checked) SwitchTool(ToolMode.SelectGoal); };
            rbMove.CheckedChanged += (s, e) => { if (rbMove.Checked) SwitchTool(ToolMode.MoveAirport); };
            rbDelete.CheckedChanged += (s, e) => { if (rbDelete.Checked) SwitchTool(ToolMode.DeleteAirport); };

            gbTools.Controls.AddRange(new Control[] { rbAddAirport, rbConnect, rbStart, rbGoal, rbMove, rbDelete });
            y += 182;

            // Velocidad
            var lblSpeed = new Label { Text = "Velocidad de Simulación:", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            sidePanel.Controls.Add(lblSpeed);
            y += 20;

            tbSpeed = new TrackBar { Location = new Point(15, y), Width = 280, Minimum = 1, Maximum = 100, Value = 40, TickFrequency = 10 };
            tbSpeed.ValueChanged += (s, e) => { searchTimer.Interval = Math.Max(10, 300 - (tbSpeed.Value * 3)); };
            sidePanel.Controls.Add(tbSpeed);
            y += 50;

            // Botones de Búsqueda
            btnStart = CreateButton("Planificar Vuelo (GBFS)", Color.FromArgb(40, 167, 69), 15, y);
            btnStart.Click += BtnStart_Click;
            sidePanel.Controls.Add(btnStart);
            y += 40;

            btnStep = CreateButton("Siguiente Escala (Paso)", Color.FromArgb(0, 123, 255), 15, y);
            btnStep.Click += BtnStep_Click;
            sidePanel.Controls.Add(btnStep);
            y += 40;

            btnReset = CreateButton("Reiniciar Búsqueda", Color.FromArgb(108, 117, 125), 15, y);
            btnReset.Click += (s, e) => ResetSearchOnly();
            sidePanel.Controls.Add(btnReset);
            y += 40;

            btnPresetNet = CreateButton("Cargar Rutas Aéreas Globales", Color.FromArgb(111, 66, 193), 15, y);
            btnPresetNet.Click += (s, e) => LoadDefaultNetwork();
            sidePanel.Controls.Add(btnPresetNet);
            y += 40;

            btnClear = CreateButton("Borrar Todo el Mapa", Color.FromArgb(220, 53, 69), 15, y);
            btnClear.Click += (s, e) => ClearTopology();
            sidePanel.Controls.Add(btnClear);
            y += 45;

            // Panel de Resultados
            lblStats = new Label
            {
                Text = "Origen: -\nDestino: -\nAeropuertos evaluados: 0\nEscalas: 0 | Distancia total: 0 km",
                Location = new Point(15, y),
                Size = new Size(280, 68),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8.5f),
                Padding = new Padding(5)
            };
            sidePanel.Controls.Add(lblStats);
            y += 75;

            var lblLogHeader = new Label { Text = "Plan de Vuelo (Escalas):", Location = new Point(15, y), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
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
            airportCounter = 1;

            // Red aérea internacional representativa con distancias de vuelo reales en kilómetros
            var aMadrid = new AirportNode("MAD", "Madrid (MAD)", 140, 260);
            var aParis = new AirportNode("CDG", "París (CDG)", 340, 150);
            var aLondres = new AirportNode("LHR", "Londres (LHR)", 280, 70);
            var aFrankfurt = new AirportNode("FRA", "Frankfurt (FRA)", 480, 140);
            var aRoma = new AirportNode("FCO", "Roma (FCO)", 360, 380);
            var aViena = new AirportNode("VIE", "Viena (VIE)", 530, 260);
            var aVarsovia = new AirportNode("WAW", "Varsovia (WAW)", 680, 140);
            var aAtenas = new AirportNode("ATH", "Atenas (ATH)", 570, 430);
            var aEstambul = new AirportNode("IST", "Estambul (IST)", 730, 360);
            var aTokio = new AirportNode("HND", "Tokio (HND)", 830, 230); // Aeropuerto destino

            // Rutas aéreas con distancias de vuelo comerciales aproximadas en kilómetros
            aMadrid.AddNeighbor(aParis, 1050);
            aMadrid.AddNeighbor(aRoma, 1360);
            aMadrid.AddNeighbor(aLondres, 1260);

            aLondres.AddNeighbor(aParis, 340);
            aLondres.AddNeighbor(aFrankfurt, 650);

            aParis.AddNeighbor(aFrankfurt, 480);
            aParis.AddNeighbor(aRoma, 1100);

            aRoma.AddNeighbor(aViena, 760);
            aRoma.AddNeighbor(aAtenas, 1050);

            aFrankfurt.AddNeighbor(aViena, 600);
            aFrankfurt.AddNeighbor(aVarsovia, 890);

            aViena.AddNeighbor(aVarsovia, 550);
            aViena.AddNeighbor(aAtenas, 1280);

            aVarsovia.AddNeighbor(aTokio, 8600); // Vuelo transcontinental norte

            aAtenas.AddNeighbor(aEstambul, 560);
            aEstambul.AddNeighbor(aTokio, 8950);

            nodes.AddRange(new[] { aMadrid, aParis, aLondres, aFrankfurt, aRoma, aViena, aVarsovia, aAtenas, aEstambul, aTokio });
            airportCounter = 11;

            startAirport = aMadrid;
            goalAirport = aTokio;

            lbExecutionLog.Items.Clear();
            lbExecutionLog.Items.Add("Rutas aéreas internacionales cargadas.");
            lbExecutionLog.Items.Add($"Origen: {startAirport.Label} -> Destino: {goalAirport.Label}");

            UpdateStats();
            canvas.Invalidate();
        }

        private void ClearTopology()
        {
            ResetSearchOnly();
            nodes.Clear();
            startAirport = null;
            goalAirport = null;
            connectionSource = null;
            airportCounter = 1;
            lbExecutionLog.Items.Clear();
            UpdateStats();
            canvas.Invalidate();
        }

        private void Canvas_MouseDown(object? sender, MouseEventArgs e)
        {
            var clickedNode = GetNodeAt(e.X, e.Y);

            switch (currentTool)
            {
                case ToolMode.AddAirport:
                    if (clickedNode == null)
                    {
                        var newAirport = new AirportNode($"A{airportCounter}", $"Aeropuerto-{airportCounter}", e.X, e.Y);
                        airportCounter++;
                        nodes.Add(newAirport);
                        if (startAirport == null) startAirport = newAirport;
                        else if (goalAirport == null) goalAirport = newAirport;
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;

                case ToolMode.ConnectAirports:
                    if (clickedNode != null)
                    {
                        if (connectionSource == null)
                        {
                            connectionSource = clickedNode;
                        }
                        else if (connectionSource != clickedNode)
                        {
                            double dist = Math.Round(connectionSource.EstimateDistanceTo(clickedNode), 0);
                            connectionSource.AddNeighbor(clickedNode, dist);
                            lbExecutionLog.Items.Add($"Ruta: {connectionSource.Label} <-> {clickedNode.Label} ({dist} km)");
                            connectionSource = null;
                            canvas.Invalidate();
                        }
                    }
                    break;

                case ToolMode.SelectStart:
                    if (clickedNode != null && clickedNode != goalAirport)
                    {
                        startAirport = clickedNode;
                        ResetSearchOnly();
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;

                case ToolMode.SelectGoal:
                    if (clickedNode != null && clickedNode != startAirport)
                    {
                        goalAirport = clickedNode;
                        ResetSearchOnly();
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;

                case ToolMode.MoveAirport:
                    if (clickedNode != null)
                    {
                        draggedNode = clickedNode;
                        dragOffset = new PointF(e.X - clickedNode.X, e.Y - clickedNode.Y);
                    }
                    break;

                case ToolMode.DeleteAirport:
                    if (clickedNode != null)
                    {
                        foreach (var other in nodes)
                        {
                            other.Neighbors.Remove(clickedNode);
                        }
                        nodes.Remove(clickedNode);
                        if (startAirport == clickedNode) startAirport = null;
                        if (goalAirport == clickedNode) goalAirport = null;
                        ResetSearchOnly();
                        UpdateStats();
                        canvas.Invalidate();
                    }
                    break;
            }
        }

        private void Canvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (currentTool == ToolMode.MoveAirport && draggedNode != null)
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

        private AirportNode? GetNodeAt(float x, float y)
        {
            const float radius = 24;
            return nodes.FirstOrDefault(n => Math.Sqrt(Math.Pow(n.X - x, 2) + Math.Pow(n.Y - y, 2)) <= radius);
        }

        private void Canvas_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Dibujar rutas aéreas
            var drawnEdges = new HashSet<(AirportNode, AirportNode)>();
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

                    // Etiqueta de distancia en el centro de la ruta
                    float midX = (node.X + neighbor.X) / 2;
                    float midY = (node.Y + neighbor.Y) / 2;

                    string distText = $"{edge.Value:F0} km";
                    using (var font = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                    {
                        var size = g.MeasureString(distText, font);
                        var badgeRect = new RectangleF(midX - size.Width / 2 - 2, midY - size.Height / 2 - 1, size.Width + 4, size.Height + 2);
                        g.FillRectangle(Brushes.White, badgeRect);
                        g.DrawRectangle(Pens.LightGray, badgeRect.X, badgeRect.Y, badgeRect.Width, badgeRect.Height);
                        g.DrawString(distText, font, Brushes.Black, midX - size.Width / 2, midY - size.Height / 2);
                    }
                }
            }

            // Si se está trazando una ruta aérea, dibujar línea temporal
            if (currentTool == ToolMode.ConnectAirports && connectionSource != null)
            {
                using (var dashedPen = new Pen(Color.FromArgb(0, 123, 255), 2f) { DashStyle = DashStyle.Dash })
                {
                    var mousePos = canvas.PointToClient(Cursor.Position);
                    g.DrawLine(dashedPen, connectionSource.X, connectionSource.Y, mousePos.X, mousePos.Y);
                }
            }

            // 2. Dibujar Aeropuertos (Nodos)
            const float radius = 24;
            foreach (var node in nodes)
            {
                Color fillColor;
                Color borderColor = Color.FromArgb(70, 70, 70);

                if (node == startAirport)
                {
                    fillColor = Color.FromArgb(40, 167, 69); // Verde: Origen / Despegue
                    borderColor = Color.FromArgb(25, 105, 44);
                }
                else if (node == goalAirport)
                {
                    fillColor = Color.FromArgb(220, 53, 69); // Rojo: Destino / Aterrizaje
                    borderColor = Color.FromArgb(145, 30, 40);
                }
                else if (optimalPath.Contains(node))
                {
                    fillColor = Color.FromArgb(111, 66, 193); // Púrpura: Escala en la ruta elegida
                    borderColor = Color.FromArgb(75, 40, 135);
                }
                else if (visited.Contains(node))
                {
                    fillColor = Color.FromArgb(150, 200, 245); // Azul suave: Aeropuerto evaluado
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

                // Círculo del aeropuerto
                var rect = new RectangleF(node.X - radius, node.Y - radius, radius * 2, radius * 2);
                using (var brush = new SolidBrush(fillColor))
                using (var pen = new Pen(borderColor, 2.5f))
                {
                    g.FillEllipse(brush, rect);
                    g.DrawEllipse(pen, rect);
                }

                // Texto dentro del círculo si es origen o destino
                if (node == startAirport)
                {
                    TextRenderer.DrawText(g, "SALIDA", new Font("Segoe UI", 7.2f, FontStyle.Bold), Rectangle.Round(rect), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else if (node == goalAirport)
                {
                    TextRenderer.DrawText(g, "META", new Font("Segoe UI", 7.2f, FontStyle.Bold), Rectangle.Round(rect), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Nombre/Etiqueta abajo del aeropuerto con fondo para evitar cruce con líneas
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

                // Valor de heurística h(n) encima del aeropuerto con recuadro ordenado
                if (goalAirport != null && node != goalAirport)
                {
                    double h = node.EstimateDistanceTo(goalAirport);
                    string hText = $"h: {h:F0} km";
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

                DrawLegendItem(g, lx + 10, ly + 26, Color.FromArgb(40, 167, 69), "Origen (Salida)", regFont);
                DrawLegendItem(g, lx + 10, ly + 44, Color.FromArgb(220, 53, 69), "Destino (Llegada)", regFont);
                DrawLegendItem(g, lx + 10, ly + 62, Color.FromArgb(255, 193, 7), "Frontera h(n)", regFont);
                DrawLegendItem(g, lx + 10, ly + 80, Color.FromArgb(150, 200, 245), "Escala evaluada", regFont);
                DrawLegendItem(g, lx + 10, ly + 98, Color.FromArgb(111, 66, 193), "Plan de vuelo voraz", regFont);
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
            if (startAirport == null || goalAirport == null)
            {
                MessageBox.Show("Debes seleccionar un aeropuerto de Origen (Salida) y un Destino (Llegada).", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!isSearching)
            {
                ResetSearchOnly();

                frontier = new PriorityQueue<FlightSearchState, double>();
                visited = new HashSet<AirportNode>();
                optimalPath.Clear();
                airportsEvaluated = 0;
                totalRealDistance = 0;

                double initialH = startAirport.EstimateDistanceTo(goalAirport);
                frontier.Enqueue(new FlightSearchState(startAirport, null, initialH, 0), initialH);

                lbExecutionLog.Items.Clear();
                lbExecutionLog.Items.Add($"Iniciando GBFS: {startAirport.Label} -> {goalAirport.Label}");
                lbExecutionLog.Items.Add($"Distancia en línea recta h = {initialH:F0} km");

                isSearching = true;
            }
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (searchTimer.Enabled)
            {
                searchTimer.Stop();
                btnStart.Text = "▶ Reanudar Vuelo";
                btnStart.BackColor = Color.FromArgb(40, 167, 69);
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
                btnStart.Text = "▶ Reanudar Vuelo";
                btnStart.BackColor = Color.FromArgb(40, 167, 69);
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
            if (frontier == null || goalAirport == null) return;

            if (frontier.Count == 0)
            {
                searchTimer.Stop();
                isSearching = false;
                btnStart.Text = "Planificar Vuelo (GBFS)";
                btnStart.BackColor = Color.FromArgb(40, 167, 69);
                lbExecutionLog.Items.Add("X No hay rutas aéreas que conecten al destino.");
                MessageBox.Show("No se encontró ruta de vuelo hacia el aeropuerto destino.", "Destino inalcanzable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentState = frontier.Dequeue();
            var currentNode = currentState.Node;

            if (visited.Contains(currentNode)) return;

            visited.Add(currentNode);
            airportsEvaluated++;

            lbExecutionLog.Items.Add($"-> Escala en '{currentNode.Label}' | h={currentState.HeuristicDistance:F0} km");
            lbExecutionLog.TopIndex = lbExecutionLog.Items.Count - 1;

            // ¿Llegamos al destino?
            if (currentNode == goalAirport)
            {
                searchTimer.Stop();
                isSearching = false;
                btnStart.Text = "Planificar Vuelo (GBFS)";
                btnStart.BackColor = Color.FromArgb(40, 167, 69);

                totalRealDistance = currentState.RealCumulativeDistance;
                ReconstructPath(currentState);

                lbExecutionLog.Items.Add($"✓ ¡Llegada al destino final!");
                lbExecutionLog.Items.Add($"Distancia real acumulada: {totalRealDistance:F0} km");
                UpdateStats();
                canvas.Invalidate();
                return;
            }

            // Evaluar aeropuertos con conexión directa
            foreach (var edge in currentNode.Neighbors)
            {
                var neighbor = edge.Key;
                double edgeDist = edge.Value;

                if (!visited.Contains(neighbor))
                {
                    double h = neighbor.EstimateDistanceTo(goalAirport); // Distancia ortodrómica en línea recta
                    double g = currentState.RealCumulativeDistance + edgeDist;

                    var childState = new FlightSearchState(neighbor, currentState, h, g);
                    frontier.Enqueue(childState, h); // Prioridad absoluta por menor distancia estimada al destino
                }
            }

            UpdateStats();
            canvas.Invalidate();
        }

        private void ReconstructPath(FlightSearchState state)
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
            btnStart.Text = "Planificar Vuelo (GBFS)";
            btnStart.BackColor = Color.FromArgb(40, 167, 69);

            frontier = null;
            visited.Clear();
            optimalPath.Clear();
            airportsEvaluated = 0;
            totalRealDistance = 0;

            UpdateStats();
            canvas.Invalidate();
        }

        private void UpdateStats()
        {
            string startStr = startAirport != null ? startAirport.Label : "No definido";
            string goalStr = goalAirport != null ? goalAirport.Label : "No definido";
            int pathHops = optimalPath.Count > 0 ? optimalPath.Count - 1 : 0;

            lblStats.Text = $"Origen: {startStr}\nDestino: {goalStr}\nAeropuertos evaluados: {airportsEvaluated}\nEscalas: {pathHops} | Distancia: {totalRealDistance:F0} km";
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
