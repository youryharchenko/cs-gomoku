using Microsoft.JSInterop;

using Excubo.Blazor.Canvas;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System;
using Radzen;

namespace gomoku.Client.Pages
{
    public partial class Game
    {
        static private int dim = 15;
        static private int d = 41;
        private int w = (dim + 1) * d;
        private int h = (dim + 1) * d;

        private double wpad = 0;
        private double hpad = 0;
        private double[] cx;
        private double[] cy;
        private int n_steps = 0;

        private Step[] steps = new Step[dim * dim];

        private bool isPlay = false;
        private bool isThink = false;
        private Net net;

        private int playerColor = 0;
        private bool disableSelect = false;
        private Canvas canvas;
        private void NewGame()
        {
            Console.WriteLine("NewGame started");

            clean();
            isPlay = true;
            disableSelect = true;
            Step st = new Step(7, 7);
            addStep(st);
            isThink = true;
            Step[] sts = new Step[] { st };

            net = new Net(sts);

            //net.stat();

            Result result;
            if (playerColor == 0)
            {
                result = net.calculate();
                //stdout.printf("new_game: calculate result: %s\n", result.step.to_string());
                addStep(result.step);
            }
            isThink = false;

            Console.WriteLine("NewGame finished");
        }

        protected override void OnInitialized()
        {
            Console.WriteLine("OnInitialized started");

            cx = new double[dim];
            cy = new double[dim];

            for (int i = 0; i < dim; i++)
            {
                cx[i] = (i + 1) * d;
                cy[i] = (i + 1) * d;
            }

            Console.WriteLine("OnInitialized finished");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await UpdateCanvasAsync();
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task UpdateCanvasAsync()
        {
            Console.WriteLine("UpdateCanvasAsync started");
            await using (var ctx = await canvas.GetContext2DAsync())
            {
                await ctx.ClearRectAsync(0, 0, w, h);
                await ctx.SetTransformAsync(1, 0, 0, 1, 0, 0);
                await ctx.RestoreAsync();
                await ctx.SaveAsync();

                //await ctx.FillTextAsync("Hello", 0, 100);
                //messages.Add("Wrote hello");
                //await ctx.FillTextAsync("canvas", 100, 10);
                //messages.Add("Wrote canvas");
                await ctx.FillStyleAsync("lightgrey");
                await ctx.FillRectAsync(wpad, hpad, w, h);
                await ctx.RectAsync(wpad, hpad, w, h);


                for (int i = 0; i < dim; i++)
                {
                    double x = wpad + cx[i];
                    await ctx.BeginPathAsync();
                    await ctx.MoveToAsync(x, hpad + d);
                    await ctx.LineToAsync(x, hpad + h - d);
                    await ctx.StrokeAsync();

                    double y = hpad + cy[i];
                    await ctx.BeginPathAsync();
                    await ctx.MoveToAsync(wpad + d, y);
                    await ctx.LineToAsync(wpad + w - d, y);
                    await ctx.StrokeAsync();
                }

                if (n_steps > 0)
                {

                    Console.WriteLine($"n_steps:{n_steps}");

                    await ctx.FontAsync("bold 16px serif");
                    await ctx.TextAlignAsync(Excubo.Blazor.Canvas.TextAlign.Center);
                    await ctx.TextBaseLineAsync(Excubo.Blazor.Canvas.TextBaseLine.Middle);

                    for (int i = 0; i < n_steps; i++)
                    {
                        int sx = steps[i].x;
                        int sy = steps[i].y;
                        double x = wpad + cx[sx];
                        double y = hpad + cy[sy];
                        int c = getStepColor(i);

                        //stdout.printf("step:%d, c:%d, x:%f, y:%f\n", i, c, x, y);

                        if (c == 0) await ctx.FillStyleAsync("black");
                        else await ctx.FillStyleAsync("white");
                        await ctx.BeginPathAsync();
                        await ctx.ArcAsync(x, y, d / 2, 0, 2 * 3.1415);
                        await ctx.FillAsync(FillRule.NonZero);

                        await ctx.StrokeStyleAsync("black");
                        await ctx.BeginPathAsync();
                        await ctx.ArcAsync(x, y, d / 2, 0, 2 * 3.1415);
                        await ctx.StrokeAsync();

                        if (c == 1) await ctx.FillStyleAsync("black");
                        else await ctx.FillStyleAsync("white");
                        string t = $"{i+1}";
                        await ctx.FillTextAsync(t, x, y);

                        /*
                        if (c == 1) cr.set_source_rgb(0.1, 0.1, 0.1);
                        else cr.set_source_rgb(0.9, 0.9, 0.9);

                        string t = @"$(i+1)";
                        Cairo.TextExtents ext;
                        cr.set_font_size(d/2 - 2);
                        cr.text_extents(t, out ext);

                        cr.move_to(x - ext.width/2 - 1, y + ext.height/2 - 1);
                        cr.show_text(t);
                        */

                    }
                }
            }
            Console.WriteLine("UpdateCanvasAsync finished");
        }

        private int getStepColor(int n)
        {
            return n % 2;
        }

        private void addStep(Step step)
        {
            Console.WriteLine($"Desk add_step:{n_steps}, c:{getStepColor(n_steps)}, x:{step.x}, y:{step.y}");
            steps[n_steps] = step;
            n_steps++;
            //queue_draw();
        }

        private void nextStep(Step step)
        {
            isThink = true;
            net.addStep(step);
            Result result = net.calculate();
            //stdout.printf("new_game: calculate result: %s\n", result.to_string());
            if (result.step != null) addStep(result.step);
            isThink = false;
            if (result.state != State.CONTINUE) gameOver(result.message);
        }

        private void gameOver(string mess)
        {
            isPlay = false;
            Console.WriteLine($"Game over: {mess}");
            disableSelect = false;
            gameOverDlg(mess);

            //set_cursor("default");
            //over_info(mess);
            //cb_color.set_sensitive(true);
            //tb_new.set_sensitive(true);
        }

        private async void  gameOverDlg(string msg)
        {
            bool? resp = await DialogService.Confirm(msg, "Game Over!", new ConfirmOptions() { OkButtonText = "New Game", CancelButtonText = "Close" });
            Console.WriteLine($"gameOverDlg: {resp.Value}");
            if(resp.Value) {
                NewGame();
                await UpdateCanvasAsync();
            }
        }


        private void clean()
        {
            for (int i = 0; i < dim * dim; i++) steps[i] = null;
            n_steps = 0;
        }

        private Step getStepFromCord(int x, int y)
        {
            int sx = -1, sy = -1;
            for (int i = 0; i < dim; i++)
            {
                if ((x >= (cx[i] + wpad - d / 2)) && (x < (cx[i] + wpad + d / 2)))
                {
                    sx = i;
                }
                if ((y >= (cy[i] + hpad - d / 2)) && (y < (cy[i] + hpad + d / 2)))
                {
                    sy = i;
                }
            }
            return new Step(sx, sy);
        }

        private bool isEmpty(int x, int y)
        {
            for (int i = 0; i < n_steps; i++)
            {
                if (steps[i].x == x && steps[i].y == y) return false;
            }
            return true;
        }


        //private void MouseDownCanvas(MouseEventArgs e)
        //{
            //render_required = false;
            //this.last_mousex = mousex = e.ClientX - canvasx;
            //this.last_mousey = mousey = e.ClientY - canvasy;
            //this.mousedown = true;
        //}

        private void MouseUpCanvas(MouseEventArgs e)
        {
            if (!isPlay || isThink) return;

            //Console.WriteLine($"MouseUpCanvas coord: ({e.OffsetX},{e.OffsetY})");
            Step step = getStepFromCord((int)e.OffsetX, (int)e.OffsetY);
            //Console.WriteLine($"MouseUpCanvas point: ({step.x},{step.y})");

            if (step.x != -1 && step.y != -1 && isEmpty(step.x, step.y))
            {
                addStep(step);
                nextStep(step);
            }
        }

        //async Task MouseMoveCanvasAsync(MouseEventArgs e)
        //{
            //render_required = false;
            //if (!mousedown)
            //{
            //    return;
            //}
            //mousex = e.ClientX - canvasx;
            //mousey = e.ClientY - canvasy;
            //await DrawCanvasAsync(mousex, mousey, last_mousex, last_mousey, clr);
            //last_mousex = mousex;
            //last_mousey = mousey;
        //}

        private class Step
        {
            public readonly int x;
            public readonly int y;

            public Step(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public string toString()
            {
                return $"Step(x:{x}, y:{y})";
            }
        }

        private class Point
        {
            public int x;
            public int y;

            public int s;
            public int[] r = new int[3];

            private Slot[] slots;
            private Net net;


            public Point(Net net, int x, int y)
            {
                this.x = x;
                this.y = y;
                this.net = net;
                this.r = new int[] { 0, 0, 0 };
                this.s = 0;
                this.slots = new Slot[] { };
            }

            public string toString()
            {
                return $"Point(x:{x}, y:{y}, s:{s}, r:[{r[0]},{r[1]},{r[2]}])";
            }

            public Slot[] getSlots()
            {
                return slots;
            }

            public void addSlot(Slot slot)
            {
                slots = slots.Append(slot).ToArray();
                r[slot.s]++;
            }
            public void printSlots()
            {
                //stdout.printf("count: %d (", slots.length);
                //foreach (Slot s in slots)
                //{
                //   stdout.printf("%d,", s.d);
                //}
                //stdout.printf(")\n");
            }

            public bool isValidScp(int d)
            {
                int mn = 1;
                int mx = dim - 2;

                // 0 - vert, 1 - horiz, 2 - up, 3 - down
                if (d == 0 && y > mn && y < mx)
                {
                    return true;
                }
                if (d == 1 && x > mn && x < mx)
                {
                    return true;
                }
                if (d == 2 && (x > mn && y < mx) && (x < mx && y > mn))
                {
                    return true;
                }
                if (d == 3 && (x > mn && y > mn) && (x < mx && y < mx))
                {
                    return true;
                }
                return false;
            }

        }

        private class Slot
        {
            public int d;
            public int r;
            public int s;
            public Point scp;
            public Point[] points;

            private Net net;


            public Slot(Net net, Point scp, int d)
            {
                this.d = d;
                this.net = net;
                this.scp = scp;
                this.points = new Point[5];
            }

            public string toString()
            {
                return $"Slot(scp:{scp}, d:{d}, s:{s}, r:{r}";
            }

            public void init()
            {
                points[2] = net.getPoint(scp.x, scp.y);
                if (d == 0)
                {
                    points[0] = net.getPoint(scp.x, scp.y - 2);
                    points[1] = net.getPoint(scp.x, scp.y - 1);
                    points[3] = net.getPoint(scp.x, scp.y + 1);
                    points[4] = net.getPoint(scp.x, scp.y + 2);
                }
                else if (d == 1)
                {
                    points[0] = net.getPoint(scp.x - 2, scp.y);
                    points[1] = net.getPoint(scp.x - 1, scp.y);
                    points[3] = net.getPoint(scp.x + 1, scp.y);
                    points[4] = net.getPoint(scp.x + 2, scp.y);
                }
                else if (d == 2)
                {
                    points[0] = net.getPoint(scp.x - 2, scp.y - 2);
                    points[1] = net.getPoint(scp.x - 1, scp.y - 1);
                    points[3] = net.getPoint(scp.x + 1, scp.y + 1);
                    points[4] = net.getPoint(scp.x + 2, scp.y + 2);
                }
                else if (d == 3)
                {
                    points[0] = net.getPoint(scp.x - 2, scp.y + 2);
                    points[1] = net.getPoint(scp.x - 1, scp.y + 1);
                    points[3] = net.getPoint(scp.x + 1, scp.y - 1);
                    points[4] = net.getPoint(scp.x + 2, scp.y - 2);
                }
                foreach (Point p in points)
                {
                    p.addSlot(this);
                }

            }
        }

        private class Net
        {
            private Slot[] allSlots;
            private Slot[] activeSlots_0;
            private Slot[] activeSlots_b;
            private Slot[] activeSlots_w;
            private Point[] allPoints;
            private Point[] emptyPoints;
            private Step[] steps;

            public Net(Step[] sts)
            {
                Console.WriteLine("Net: constructor started");

                this.allSlots = new Slot[0];

                this.activeSlots_b = new Slot[0];
                this.activeSlots_w = new Slot[0];
                this.allPoints = new Point[dim * dim];
                this.emptyPoints = new Point[dim * dim];
                this.steps = new Step[0];

                int countSlots = 0;
                for (int i = 0; i < (dim * dim); i++)
                {
                    Point p = new Point(this, (int)(i / dim), i % dim);
                    this.allPoints[i] = p;
                    this.emptyPoints[i] = p;
                    for (int d = 0; d < 4; d++)
                    {
                        if (p.isValidScp(d))
                        {
                            //Console.WriteLine($"Net: countSlots = {countSlots}");
                            Slot s = new Slot(this, p, d);
                            allSlots = allSlots.Append(s).ToArray();
                            countSlots++;
                            //stdout.printf("slot d:%d, scp:(%d, %d)\n", d, p.x, p.y);
                        }
                    }
                }

                Console.WriteLine($"Net: countSlots = {countSlots}");
                Console.WriteLine($"Net: allSlots.Length = {allSlots.Length}");

                this.activeSlots_0 = new Slot[countSlots];
                for (int i = 0; i < countSlots; i++)
                {
                    this.allSlots[i].init();
                    this.activeSlots_0[i] = allSlots[i];
                }

                stat();

                Console.WriteLine($"Net: sts.Length = {sts.Length}");
                foreach (Step s in sts)
                {
                    Console.WriteLine($"Net: st = {s.toString()}");
                    addStep(s);
                }

                Console.WriteLine("Net: constructor finsshed");
            }

            public void stat()
            {
                Console.WriteLine("*** Net: stat");

                Console.WriteLine($"steps.Length = {steps.Length}");
                Console.WriteLine($"emptyPoints.Length = {emptyPoints.Length}");
                Console.WriteLine($"activeSlots_0.Length = {activeSlots_0.Length}");
                Console.WriteLine($"activeSlots_b.Length = {activeSlots_b.Length}");
                Console.WriteLine($"activeSlots_w.Length = {activeSlots_w.Length}");

                Console.WriteLine("***");
            }

            public Point getPoint(int x, int y)
            {
                return allPoints[x * dim + y];
            }

            public void addStep(Step step)
            {
                Console.WriteLine($"Net: addStep = {step.toString()}");

                Point p = getPoint(step.x, step.y);
                int c = steps.Length % 2;

                //stdout.printf("Net: add step start: n:%d, c: %d, %s  will be added \n", steps.length, c, p.to_string());

                p.s = c + 1;
                //int n = findPoint(emptyPoints, p);
                //if (n < 0)
                //{
                //stdout.printf("Net: add step error: %s is not fount in empty_points\n", p.to_string());
                //    return;
                //}
                //stdout.printf("Net: add step: empty_points before len:%d\n", empty_points.length);
                //empty_points = remove_point(empty_points, n);
                emptyPoints = emptyPoints.Where(a => !(a.x == p.x && a.y == p.y)).ToArray();
                //emptyPoints.move(n + 1, n, empty_points.length - n - 1);
                //empty_points.resize(empty_points.length - 1);
                //stdout.printf("Net: add step: empty_points after len:%d\n", empty_points.length);

                //stdout.printf("Net: add step: point slots len:%d\n", p.get_slots().length);
                int i = 0;
                foreach (Slot s in p.getSlots())
                {
                    //stdout.printf("Net: add step: slot:%d %s\n", i, s.to_string());
                    if (s.s == 0)
                    {
                        //stdout.printf("Net: add step: v:%d\n", 1);
                        p.r[0]--;
                        p.r[c + 1]++;
                        s.s = c + 1;
                        s.r = 1;

                        //int m = findSlot(active_slots_0, s);
                        //if (m < 0)
                        //{
                        //    stdout.printf("Net: add step error: slot (%d, %d) is not fount in active_slots_0\n", s.scp.x, s.scp.y);
                        //    return;
                        //}
                        //active_slots_0 = remove_slot(active_slots_0, m);
                        activeSlots_0 = activeSlots_0.Where(a => !(a.d == s.d && a.scp.x == s.scp.x && a.scp.y == s.scp.y)).ToArray();
                        //active_slots_0.move(m + 1, m, active_slots_0.length - m - 1);
                        //active_slots_0.resize(active_slots_0.length - 1);
                        if (c == 0)
                        {
                            //active_slots_b = append_slot(active_slots_b, s);
                            activeSlots_b = activeSlots_b.Append(s).ToArray();
                        }
                        if (c == 1)
                        {
                            //active_slots_w = append_slot(active_slots_w, s);
                            activeSlots_w = activeSlots_w.Append(s).ToArray();
                        }
                    }
                    else if (s.s == (c + 1))
                    {
                        //stdout.printf("Net: add step: v:%d\n", 2);
                        p.r[c + 1]++;
                        s.r++;
                    }
                    else if (s.s != 3)
                    {
                        //stdout.printf("Net: add step: v:%d\n", 3);
                        p.r[c + 1]--;
                        if (s.s == 1)
                        {
                            //int m = find_slot(active_slots_b, s);
                            //if (m < 0)
                            //{
                            //    stdout.printf("Net: add step error: slot (%d, %d) is not fount in active_slots_b\n", s.scp.x, s.scp.y);
                            //    return;
                            //}
                            //active_slots_b = remove_slot(active_slots_b, m);
                            activeSlots_b = activeSlots_b.Where(a => !(a.d == s.d && a.scp.x == s.scp.x && a.scp.y == s.scp.y)).ToArray();
                            //active_slots_b.move(m + 1, m, active_slots_b.length - m - 1);
                            //active_slots_b.resize(active_slots_b.length - 1);
                        }
                        if (s.s == 2)
                        {
                            //int m = find_slot(active_slots_w, s);
                            //if (m < 0)
                            //{
                            //    stdout.printf("Net: add step error: slot (%d, %d) is not fount in active_slots_w\n", s.scp.x, s.scp.y);
                            //    return;
                            //}
                            //active_slots_w = remove_slot(active_slots_w, m);
                            activeSlots_w = activeSlots_w.Where(a => !(a.d == s.d && a.scp.x == s.scp.x && a.scp.y == s.scp.y)).ToArray();
                            //active_slots_w.move(m + 1, m, active_slots_w.length - m - 1);
                            //active_slots_w.resize(active_slots_w.length - 1);
                        }
                        s.s = 3;
                    }
                    else
                    {
                        //stdout.printf("Net: add step: v:%d\n", 4);
                    }
                    i++;
                }
                //steps = append_step(steps, step);
                steps = steps.Append(step).ToArray();

                stat();
                //stdout.printf("Net: add step end: n:%d, c: %d, %s  will be added \n", steps.length, c, p.to_string());
            }

            public Result calculate()
            {
                Console.WriteLine("Net: calculate started");

                if (checkWin())
                {
                    Console.WriteLine("Net: calculate before win");
                    return new Result(State.WIN, null, "You won!");
                }
                if (checkDraw())
                {
                    Console.WriteLine("Net: calculate before draw");
                    return new Result(State.DRAW, null, "Draw!");
                }

                Step newStep = calcPoint();
                Console.WriteLine($"Net: calculated step: {newStep.toString()}");
                addStep(newStep);

                if (checkWin())
                {
                    Console.WriteLine("Net: calculate after win\n");
                    return new Result(State.WIN, newStep, "I won!");
                }
                if (checkDraw())
                {
                    Console.WriteLine("Net: calculate after draw");
                    return new Result(State.DRAW, newStep, "Draw!");
                }
                Result res = new Result(State.CONTINUE, newStep, "");
                Console.WriteLine("Net: calculate finished");
                return res;
            }

            private bool checkWin()
            {
                Console.WriteLine("Net: check win start");

                //foreach (Slot s in activeSlots_b)
                //{
                    //if (s == null)
                    //{
                    //stdout.printf("Net: active_slots_b slot is null\n");
                    //    return false;
                    //}
                    //if (s.r == 5)
                    //{
                    //    return true;
                    //}
                //}
                if(activeSlots_b.Where(a => a.r == 5).Count() > 0) return true;
                //foreach (Slot s in activeSlots_w)
                //{
                    //if (s == null)
                    //{
                    //stdout.printf("Net: active_slots_w slot is null\n");
                    //    return false;
                    //}
                    //if (s.r == 5)
                    //{
                    //    return true;
                    //}
                //}
                if(activeSlots_w.Where(a => a.r == 5).Count() > 0) return true;
                return false;
            }
            private bool checkDraw()
            {
                Console.WriteLine("Net: check draw start");

                if (activeSlots_0.Length == 0 && activeSlots_b.Length == 0 && activeSlots_w.Length == 0)
                    return true;

                return false;
            }

            private Step calcPoint()
            {
                Console.WriteLine("calcPoint started");

                int c = steps.Length % 2;
                Point[] points;
                //stdout.printf("Net: calc_point start\n");

                points = findSlot_4(c);
                if (points.Length == 0) points = findSlot_4(1 - c);
                if (points.Length == 0) points = findPoint_x(c, 2, 1);
                if (points.Length == 0) points = findPoint_x(1 - c, 2, 1);
                if (points.Length == 0) points = findPoint_x(c, 1, 5);
                if (points.Length == 0) points = findPoint_x(1 - c, 1, 5);
                if (points.Length == 0) points = findPoint_x(c, 1, 4);
                if (points.Length == 0) points = findPoint_x(1 - c, 1, 4);
                if (points.Length == 0) points = findPoint_x(c, 1, 3);
                if (points.Length == 0) points = findPoint_x(1 - c, 1, 3);
                if (points.Length == 0) points = findPoint_x(c, 1, 2);
                if (points.Length == 0) points = findPoint_x(1 - c, 1, 2);
                if (points.Length == 0) points = findPoint_x(c, 1, 1);
                if (points.Length == 0) points = findPoint_x(1 - c, 1, 1);
                if (points.Length == 0) points = findPoint_x(c, 0, 10);
                if (points.Length == 0) points = findPoint_x(1 - c, 0, 10);
                if (points.Length == 0) points = findPoint_x(c, 0, 9);
                if (points.Length == 0) points = findPoint_x(1 - c, 0, 9);
                if (points.Length == 0) points = findPoint_x(c, 0, 8);
                if (points.Length == 0) points = findPoint_x(1 - c, 0, 8);
                if (points.Length == 0) points = findPoint_x(c, 0, 7);
                if (points.Length == 0) points = findPoint_x(1 - c, 0, 7);
                if (points.Length == 0) points = calcPointMaxRate(c);

                Point res = points[new System.Random().Next(points.Length)];

                Console.WriteLine("calcPoint finished");
                return new Step(res.x, res.y);
            }

            private Point[] findSlot_4(int c)
            {
                //msg := fmt.Sprintf("%v :: find_slot_4(%v,%v)", c)
                if (c == 0)
                {
                    foreach (Slot s in activeSlots_b)
                    {
                        if (s.r == 4)
                        {
                            foreach (Point p in s.points)
                            {
                                if (p.s == 0)
                                {
                                    //stdout.printf("Net: find_slot_4 b point:%s\n", p.to_string());
                                    return new Point[] { p };
                                }
                            }
                        }
                    }
                }
                if (c == 1)
                {
                    foreach (Slot s in activeSlots_w)
                    {
                        if (s.r == 4)
                        {
                            foreach (Point p in s.points)
                            {
                                if (p.s == 0)
                                {
                                    //stdout.printf("Net: find_slot_4 w point:%s\n", p.to_string());
                                    return new Point[] { p };
                                }
                            }
                        }
                    }
                }
                return new Point[0];
            }

            private Point[] findPoint_x(int c, int r, int b)
            {
                //msg := fmt.Sprintf("%v :: find_point_x(%v,%v)", c, r, b)
                Point[] result = new Point[0];
                foreach (Point p in emptyPoints)
                {
                    int i = 0;
                    foreach (Slot s in p.getSlots())
                    {
                        if (s.s == (c + 1) && s.r > r)
                        {
                            i++;
                            if (i > b)
                            {
                                result = result.Append(p).ToArray();
                            }
                        }
                    }
                }
                //if (result.length > 0) stdout.printf("Net: find_point_x r:%d, b:%d, points.lenght:%d\n", r, b, result.length);
                return result;
            }

            private Point[] calcPointMaxRate(int c)
            {
                int r = -1;
                int d = 0;
                int i = 0;

                Point[] result = new Point[] { };

                foreach (Point p in emptyPoints)
                {
                    d = 0;
                    foreach (Slot s in p.getSlots())
                    {
                        if (s.s == 0)
                        {
                            d += 1;
                        }
                        else if (s.s == (c + 1))
                        {
                            d += (1 + s.r) * (1 + s.r);
                        }
                        else if (s.s != 3)
                        {
                            d += (1 + s.r) * (1 + s.r);
                        }
                    }
                    if (d > r)
                    {
                        i = 1;
                        r = d;
                        result = new Point[] { };
                        //result = append_point(result, p);
                        result = result.Append(p).ToArray();
                        //msg = fmt.Sprintf("%v :: point_max_rate(%v,%v) -> (%v, %v)", c, i, r, elm[0], elm[1])
                    }
                    else if (d == r)
                    {
                        i++;
                        //result = append_point(result, p);
                        result = result.Append(p).ToArray();
                        //msg = fmt.Sprintf("%v :: point_max_rate(%v,%v) -> (%v, %v)", c, i, r, elm[0], elm[1])
                    }
                }
                //stdout.printf("Net: calc_point_max_rate points.length:%d\n", result.length);
                return result;
            }

        }

        private enum State
        {
            CONTINUE,
            DRAW,
            WIN
        }
        private class Result
        {
            public State state;
            public Step step;
            public string message;

            public Result(State state, Step step, string message)
            {
                this.state = state;
                this.step = step;
                this.message = message;
            }

            public string stateToString(State st)
            {
                switch (st)
                {
                    case State.CONTINUE:
                        return "CONTINUE";
                    case State.WIN:
                        return "WIN";
                    case State.DRAW:
                        return "DRAW";
                }
                return "UNKNOWN";
            }

            public string toString()
            {
                if (step != null)
                    return @"Result(step:$step, state: $(state_to_string(state)))";
                else
                    return @"Result(step:null, state: $(state_to_string(state)))";
            }
        }
    }
}