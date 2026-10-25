using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Graphics;
namespace DynastyManager.App;
public sealed class GameFieldDrawable : IDrawable
    {
        public double HashMarkFraction { get; set; } = .30;
        public int YardLine { get; set; } = 25;
        public int Distance { get; set; } = 10;
        public int Direction { get; set; } = 1;
        public Color HomeColor { get; set; } = Colors.DarkBlue;
        public Color HomeSecondaryColor { get; set; } = Colors.White;
        public string HomeInitial { get; set; } = "H";
        public string HomeName { get; set; } = "HOME";
        public bool CoinCeremony { get; set; }
        public GameWeather Weather { get; set; } = new();
        public double WeatherPhase { get; set; }

        public void Draw(ICanvas canvas, RectF rect)
        {
            canvas.FillColor = DynastyTheme.Palette("#245B38"); canvas.FillRectangle(rect);
            // Stable procedural turf: mowing bands, subtle blades and five-yard lines.
            for (var band = 0; band < 20; band++)
            {
                canvas.FillColor = Color.FromArgb(band % 2 == 0 ? "#2E6944" : "#285D3D");
                canvas.FillRectangle(rect.Width * (10 + band * 5) / 120f, 0, rect.Width * 5 / 120f, rect.Height);
            }
            for (var blade = 0; blade < 900; blade++)
            {
                var x = rect.Width * ((blade * 73 % 997) / 997f);
                var y = rect.Height * ((blade * 139 % 991) / 991f);
                canvas.FillColor = blade % 2 == 0 ? Color.FromRgba(1, 1, 1, .035) : Color.FromRgba(0, 0, 0, .045);
                canvas.FillRectangle(x, y, 1, 2);
            }
            canvas.StrokeColor = Color.FromRgba(1, 1, 1, .25); canvas.StrokeSize = .7f;
            for (var yard = 5; yard < 100; yard += 5)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, 0, x, rect.Height);
            }
            canvas.FillColor = HomeColor;
            var leftGoal = (float)GameDayAnimationPlanner.ProjectYardLine(0, 1);
            var rightGoal = (float)GameDayAnimationPlanner.ProjectYardLine(100, 1);
            canvas.FillRectangle(0, 0, rect.Width * leftGoal, rect.Height);
            canvas.FillRectangle(rect.Width * rightGoal, 0, rect.Width * leftGoal, rect.Height);
            canvas.SaveState(); canvas.FontColor = HomeSecondaryColor; canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = Math.Clamp(rect.Width * leftGoal * .35f, 10, 23);
            canvas.Translate(rect.Width * leftGoal * .5f, rect.Height * .5f); canvas.Rotate(-90);
            canvas.DrawString(HomeName, -rect.Height / 2, -rect.Width * leftGoal / 2, rect.Height, rect.Width * leftGoal,
                HorizontalAlignment.Center, VerticalAlignment.Center); canvas.RestoreState();
            canvas.SaveState(); canvas.FontColor = HomeSecondaryColor; canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = Math.Clamp(rect.Width * leftGoal * .35f, 10, 23);
            canvas.Translate(rect.Width * (rightGoal + leftGoal * .5f), rect.Height * .5f); canvas.Rotate(90);
            canvas.DrawString(HomeName, -rect.Height / 2, -rect.Width * leftGoal / 2, rect.Height, rect.Width * leftGoal,
                HorizontalAlignment.Center, VerticalAlignment.Center); canvas.RestoreState();
            canvas.StrokeColor = Colors.White; canvas.StrokeSize = 2;
            canvas.DrawRectangle(1, 1, rect.Width - 2, rect.Height - 2);
            canvas.DrawLine(rect.Width * leftGoal, 0, rect.Width * leftGoal, rect.Height);
            canvas.DrawLine(rect.Width * rightGoal, 0, rect.Width * rightGoal, rect.Height);
            canvas.StrokeColor = DynastyTheme.Palette("#FFD447"); canvas.StrokeSize = 4;
            var leftPost = rect.Width * .026f;
            var rightPost = rect.Width * .974f;
            var postTop = rect.Height * .41f;
            var postBottom = rect.Height * .59f;
            canvas.DrawLine(0, rect.Center.Y, leftPost, rect.Center.Y);
            canvas.DrawLine(leftPost, postTop, leftPost, postBottom);
            canvas.DrawLine(leftPost, postTop, rect.Width * .046f, postTop);
            canvas.DrawLine(leftPost, postBottom, rect.Width * .046f, postBottom);
            canvas.DrawLine(rect.Width, rect.Center.Y, rightPost, rect.Center.Y);
            canvas.DrawLine(rightPost, postTop, rightPost, postBottom);
            canvas.DrawLine(rightPost, postTop, rect.Width * .954f, postTop);
            canvas.DrawLine(rightPost, postBottom, rect.Width * .954f, postBottom);
            canvas.StrokeColor = DynastyTheme.Palette("#A8CBB5"); canvas.StrokeSize = 1;
            for (var yard = 10; yard < 100; yard += 10)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, 0, x, rect.Height);
                canvas.FontColor = DynastyTheme.Palette("#D5E6DB"); canvas.FontSize = 11;
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, 8, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
                canvas.DrawString((yard <= 50 ? yard : 100 - yard).ToString(), x - 10, rect.Height - 26, 24, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            canvas.StrokeColor = DynastyTheme.Palette("#D5E6DB"); canvas.StrokeSize = 1;
            for (var yard = 1; yard < 100; yard++)
            {
                var x = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(yard, 1);
                canvas.DrawLine(x, rect.Height * (float)(HashMarkFraction - .01), x, rect.Height * (float)(HashMarkFraction + .01));
                canvas.DrawLine(x, rect.Height * (float)(1 - HashMarkFraction - .01), x, rect.Height * (float)(1 - HashMarkFraction + .01));
            }
            canvas.FillColor = HomeColor;
            canvas.FillCircle(rect.Center.X, rect.Center.Y, Math.Min(rect.Width, rect.Height) * .105f);
            canvas.StrokeColor = HomeSecondaryColor; canvas.StrokeSize = 2;
            canvas.DrawCircle(rect.Center.X, rect.Center.Y, Math.Min(rect.Width, rect.Height) * .105f);
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = Math.Min(46, Math.Min(rect.Width, rect.Height) * .16f) / Math.Max(1, HomeInitial.Length * .6f); canvas.FontColor = HomeSecondaryColor;
            canvas.DrawString(HomeInitial, rect.Width * .40f, rect.Height * .36f, rect.Width * .20f, rect.Height * .28f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            var line = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(YardLine, Direction);
            var gain = rect.Width * (float)GameDayAnimationPlanner.ProjectYardLine(Math.Min(100, YardLine + Distance), Direction);
            canvas.StrokeColor = DynastyTheme.Palette("#4FA8FF"); canvas.StrokeSize = 3; canvas.DrawLine(line, 0, line, rect.Height);
            canvas.StrokeColor = DynastyTheme.Palette("#E7C45B"); canvas.StrokeSize = 3; canvas.DrawLine(gain, 0, gain, rect.Height);
            if (!CoinCeremony)
            {
                var officialX = rect.Width * (float)GameDayAnimationPlanner.ProjectFieldYardLine(Math.Clamp(YardLine - 12, 5, 95), Direction);
                FootballOfficialDrawable.Paint(canvas, officialX, rect.Height * .55f, .7f);
                var sidelineX = rect.Width * (float)GameDayAnimationPlanner.ProjectFieldYardLine(YardLine, Direction);
                FootballOfficialDrawable.Paint(canvas, sidelineX, rect.Height * .045f, .65f);
                FootballOfficialDrawable.Paint(canvas, sidelineX, rect.Height * .955f, .65f);
            }
            if (!Weather.Indoors && Weather.Precipitation != GamePrecipitation.Clear)
            {
                var snow = Weather.Precipitation == GamePrecipitation.Snow;
                canvas.FillColor = Color.FromRgba(1, 1, 1, .75);
                canvas.StrokeColor = Color.FromRgba(.75, .88, 1, .65); canvas.StrokeSize = 1;
                for (var i = 0; i < 100; i++)
                {
                    var x = rect.Width * (float)((i * .618033 + WeatherPhase * .07 * Weather.WindFieldDirection + 10) % 1);
                    var y = rect.Height * (float)((i * .41321 + WeatherPhase * (snow ? .18 : .8)) % 1);
                    if (snow) canvas.FillCircle(x, y, 1.7f);
                    else canvas.DrawLine(x, y, x + Weather.WindFieldDirection * 3, y + 9);
                }
            }
        }
    }
