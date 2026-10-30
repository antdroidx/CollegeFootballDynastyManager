using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Graphics;
namespace DynastyManager.App;

public sealed class PlayArtDrawable(string id, string playName, bool offense, string styleKey, OffensivePlayDefinition? definition = null, DefensivePlayDefinition? defensiveDefinition = null) : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float diagramWidth = 185, diagramHeight = 112;
        var scale = Math.Min(dirtyRect.Width / diagramWidth, dirtyRect.Height / diagramHeight);
        canvas.SaveState();
        canvas.Translate(dirtyRect.X + (dirtyRect.Width - diagramWidth * scale) / 2,
            dirtyRect.Y + (dirtyRect.Height - diagramHeight * scale) / 2);
        canvas.Scale(scale, scale);
        DrawDiagram(canvas, new RectF(0, 0, diagramWidth, diagramHeight));
        canvas.RestoreState();
    }

    private void DrawDiagram(ICanvas canvas, RectF dirtyRect)
    {
        var width = dirtyRect.Width;
        var height = dirtyRect.Height;
        canvas.StrokeColor = DynastyTheme.Palette("#3E675B");
        canvas.StrokeSize = 1;
        for (var yard = 1; yard < 5; yard++)
        {
            var y = height * yard / 5f;
            canvas.DrawLine(0, y, width, y);
        }
        canvas.StrokeColor = DynastyTheme.Palette("#D8E5E2");
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(8, height * .58f, width - 8, height * .58f);

        var seed = id.Aggregate(0, (hash, character) => (hash * 31 + character) & int.MaxValue);
        if (offense) DrawOffense(canvas, width, height, id, definition);
        else if (defensiveDefinition?.Assignments.Count > 0)
        {
            canvas.StrokeColor = DynastyTheme.Palette("#7CC6FF"); canvas.StrokeSize = 2;
            foreach (var a in defensiveDefinition.Assignments)
            {
                var x = width * (float)a.Lane; var y = height * (.55f - (float)a.Depth * .014f);
                canvas.DrawCircle(x, y, 4);
                if (a.Task == AssignmentTask.Rush) DrawRoute(canvas, x, y, x, height * .68f);
                else if (a.Task == AssignmentTask.Zone) canvas.DrawEllipse(x - 10, y - 15, 20, 12);
                else canvas.DrawLine(x, y, x + 8, y - 15);
            }
        }
        else DrawDefense(canvas, width, height, seed, styleKey, playName);
    }

    private static void DrawOffense(ICanvas canvas, float width, float height, string playId, OffensivePlayDefinition? definition)
    {
        var play = definition ?? PlaybookCatalog.OffensivePlays.FirstOrDefault(item => item.Id == playId);
        if (play is null) return;
        var family = GameDayAnimationPlanner.RouteFamily(play);
        canvas.FillColor = DynastyTheme.Palette("#F2F5F4");
        for (var player = 0; player < 5; player++)
        {
            var lineman = GameDayAnimationPlanner.OffensiveAlignment(play, Position.OL, player);
            canvas.FillCircle(width * (float)(lineman.Y), height * (.61f - (float)(lineman.X) * .03f), 3);
        }
        var qb = play.Assignments.FirstOrDefault(a => a.Position == Position.QB && a.RoleIndex == 0);
        var quarterback = GameDayAnimationPlanner.OffensiveAlignment(play, Position.QB, 0);
        canvas.FillCircle(width * (float)quarterback.Y, height * (.61f - (float)quarterback.X * .03f), 3.5f);
        var positions = GameDayAnimationPlanner.EligiblePositions(play.Personnel);
        var roleCounts = new Dictionary<Position, int>();
        for (var i = 0; i < positions.Count; i++)
        {
            var position = positions[i];
            roleCounts.TryGetValue(position, out var roleIndex);
            roleCounts[position] = roleIndex + 1;
            var custom = play.Assignments.FirstOrDefault(a => a.Position == position && a.RoleIndex == roleIndex);
            var alignment = GameDayAnimationPlanner.OffensiveAlignment(play, position, roleIndex);
            var assigned = GameDayAnimationPlanner.AssignedRoute(play, position, roleIndex);
            var start = new GameFieldPoint(0, alignment.Y);
            var depth = GameDayAnimationPlanner.RouteDepthShare(assigned, position, false);
            var end = new GameFieldPoint(depth, GameDayAnimationPlanner.RouteEndLane(assigned, position, i, start.Y, false));
            var startY = .61f - (float)alignment.X * .03f;
            canvas.FillColor = DynastyTheme.Palette("#F2F5F4");
            canvas.FillCircle(width * (float)start.Y, height * startY, 3.5f);
            if (custom?.Task == AssignmentTask.Block) continue;
            if (family == GameDayRouteFamily.Run)
            {
                if (roleIndex == 0 && position == (play.Name.StartsWith("FB ") ? Position.FB : Position.RB))
                {
                    canvas.StrokeColor = DynastyTheme.Palette("#F4CC66"); canvas.StrokeSize = 2;
                    DrawRoute(canvas, width * (float)start.Y, height * startY,
                        width * (play.Concept == OffensiveConcept.OutsideRun ? .80f : .50f), height * .38f);
                }
                continue;
            }
            canvas.StrokeColor = DynastyTheme.Palette("#F4CC66");
            canvas.StrokeSize = 1.8f;
            var previousX = width * (float)start.Y;
            var previousY = height * startY;
            for (var step = 1; step <= 10; step++)
            {
                var point = GameDayAnimationPlanner.RoutePoint(start, end, assigned, step / 10d);
                var nextX = width * (float)point.Y;
                var nextY = height * (startY - (float)point.X * .52f);
                canvas.DrawLine(previousX, previousY, nextX, nextY);
                if (step == 10) DrawRoute(canvas, previousX, previousY, nextX, nextY);
                previousX = nextX; previousY = nextY;
            }
        }
    }

    private static void DrawRoute(ICanvas canvas, float x1, float y1, float x2, float y2)
    {
        canvas.DrawLine(x1, y1, x2, y2);
        canvas.DrawLine(x2, y2, x2 - 4, y2 + 7);
        canvas.DrawLine(x2, y2, x2 + 5, y2 + 5);
    }

    private static void DrawDefense(ICanvas canvas, float width, float height, int seed, string approach, string name)
    {
        canvas.StrokeColor = DynastyTheme.Palette("#7CC6FF");
        canvas.StrokeSize = 2;
        var pressure = approach.Contains("Pressure", StringComparison.OrdinalIgnoreCase);
        var protect = approach.Contains("Protect", StringComparison.OrdinalIgnoreCase);
        for (var player = 0; player < 4; player++)
        {
            var x = width * (.32f + player * .12f);
            canvas.DrawCircle(x, height * .49f, 4);
            if (pressure || player < 2) canvas.DrawLine(x, height * .44f, x + (player % 2 == 0 ? -6 : 6), height * .31f);
        }
        for (var player = 0; player < 3; player++)
            canvas.DrawCircle(width * (.3f + player * .2f), height * .34f, 4);
        canvas.DrawCircle(width * .18f, height * .2f, 4);
        canvas.DrawCircle(width * .82f, height * .2f, 4);
        if (name.Contains("Cover 3", StringComparison.OrdinalIgnoreCase))
        {
            canvas.StrokeColor = DynastyTheme.Palette("#99E0D0");
            for (var zone = 0; zone < 3; zone++)
                canvas.DrawArc(width * (.06f + zone * .30f), height * .06f, width * .28f, height * .30f, 190, 160, false, false);
        }
        else if (name.Contains("Quarters", StringComparison.OrdinalIgnoreCase))
        {
            canvas.StrokeColor = DynastyTheme.Palette("#99E0D0");
            for (var zone = 0; zone < 4; zone++)
                canvas.DrawArc(width * (.03f + zone * .24f), height * .07f, width * .22f, height * .27f, 190, 160, false, false);
        }
        else if (protect)
        {
            canvas.StrokeColor = DynastyTheme.Palette("#99E0D0");
            canvas.DrawArc(width * .1f, height * .08f, width * .8f, height * .32f, 190, 160, false, false);
        }
        else if (seed % 2 == 0)
            canvas.DrawLine(width * .3f, height * .3f, width * .48f, height * .6f);
    }
}
