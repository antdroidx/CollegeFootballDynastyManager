using DynastyManager.Core.Simulation;
using Microsoft.Maui.Graphics;

namespace DynastyManager.App;

/// <summary>Original shaded three-quarter pixel player. No bitmap sheets, downloads or simulation state.</summary>
public sealed class FootballSpriteView : GraphicsView
{
    private readonly PlayerDrawable art;
    private int lastFrame = -1;
    private readonly int stridePhase;
    public FootballSpriteView(Color primary, Color secondary, bool away, int skin, string label)
    {
        art = new(primary, secondary, away, skin) { Label = label };
        stridePhase = (label.Sum(c => c) + skin) % 4;
        Drawable = art; InputTransparent = true;
        WidthRequest = HeightRequest = 28;
    }
    public void HuddleFacing(double dx, double dy) { art.Frame = 1; art.Heading = GameDayAnimationPlanner.SpriteHeading(dx, dy, 90); Invalidate(); }
    public void SetLabel(string label) { art.Label = label; Invalidate(); }
    public void Highlight(bool active) { art.Active = active; Invalidate(); }
    public void Animate(double progress, double seconds, double dx, double dy, bool blocking, bool passing)
    {
        var frame = ((int)(progress * seconds * 8) + stridePhase) % 4;
        var heading = GameDayAnimationPlanner.SpriteHeading(dx, dy, art.Heading);
        if (frame == lastFrame && heading == art.Heading && blocking == art.Blocking && passing == art.Passing) return;
        lastFrame = frame; art.Frame = frame; art.Heading = heading; art.Blocking = blocking; art.Passing = passing;
        Invalidate();
    }
    private sealed class PlayerDrawable(Color primary, Color secondary, bool away, int skin) : IDrawable
    {
        public string Label { get; set; } = "";
        public bool Active { get; set; }
        public double Heading { get; set; } = 90;
        public bool Blocking { get; set; }
        public bool Passing { get; set; }
        public int Frame { get; set; }
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) / 64f;
            canvas.SaveState(); canvas.Translate(dirtyRect.X, dirtyRect.Y); canvas.Scale(scale, scale);
            // Upright three-quarter camera: facing changes the pose, never rotates a person onto their side.
            var radians = (Heading - 90) * Math.PI / 180;
            var fx = Math.Cos(radians); var fy = Math.Sin(radians);
            var sx = -fy; var sy = fx * .45;
            var front = fy >= -.3;
            var ink = Color.FromArgb("#101923");
            var skinColor = Color.FromArgb(new[] { "#E9B98E", "#C78B60", "#956343", "#65432F" }[Math.Clamp(skin, 0, 3)]);
            var jersey = away ? Colors.White : primary;
            var pants = away ? Color.FromArgb("#E3E7EB") : secondary;
            Color Shade(Color c, float amount) => new(c.Red * amount, c.Green * amount, c.Blue * amount);
            void Box(Color color, double x, double y, double w, double h)
            { canvas.FillColor = color; canvas.FillRectangle((float)Math.Round(x), (float)Math.Round(y), (float)Math.Ceiling(w), (float)Math.Ceiling(h)); }
            void Oval(Color color, int x, int y, int w, int h)
            {
                for (var row = 0; row < h; row++)
                {
                    var v = (row + .5 - h / 2d) / (h / 2d);
                    var half = Math.Sqrt(Math.Max(0, 1 - v * v)) * w / 2;
                    Box(color, x + w / 2d - half, y + row, half * 2, 1);
                }
            }
            void Limb(Color color, double ax, double ay, double bx, double by, int width)
            {
                var steps = Math.Max(1, (int)(Math.Max(Math.Abs(bx-ax), Math.Abs(by-ay)) / 2));
                for (var step = 0; step <= steps; step++)
                    Box(color, ax + (bx-ax)*step/steps-width/2d, ay+(by-ay)*step/steps-width/2d, width, width);
            }
            void OutlinedLimb(Color color, double ax, double ay, double bx, double by, int width)
            { Limb(ink, ax, ay, bx, by, width+2); Limb(Shade(color,.7f),ax,ay,bx,by,width); Limb(color,ax-1,ay-1,bx-1,by-1,Math.Max(2,width-2)); }
            canvas.FillColor = Color.FromRgba(0,0,0,.26); canvas.FillEllipse(15,49,35,11);
            if (Active) { canvas.StrokeColor=Color.FromArgb("#FFDE59"); canvas.StrokeSize=2; canvas.DrawEllipse(11,47,43,14); }
            var stride = Blocking ? (Frame % 2 == 0 ? 1d : -1d) : Frame switch { 0 => -7d, 2 => 7d, _ => 0d };
            var lean = Passing ? 1 : Blocking ? 3 : 2;
            var bodyX = 32 + fx * lean;
            // Far leg first. Feet advance along the ground heading; the torso stays upright.
            for (var leg=0; leg<2; leg++)
            {
                var side = leg==0 ? -1 : 1; var gait = stride*side;
                var hipX=32+sx*side*5; var hipY=40+sy*side*5;
                var footX=32+sx*side*7+fx*gait; var footY=54+sy*side*7+fy*gait*.45;
                var kneeX=(hipX+footX)/2; var kneeY=47-Math.Max(0,gait)*.5;
                OutlinedLimb(pants,hipX,hipY,kneeX,kneeY,7);
                OutlinedLimb(pants,kneeX,kneeY,footX,footY-3,6);
                Box(Colors.White,footX-3,footY-5,6,3);
                Box(ink,footX-4+fx*2,footY-1,9,4); Box(Color.FromArgb("#667383"),footX-3,footY-1,4,1);
            }
            var torsoWidth=Math.Abs(fy)>.6?22:18;
            Box(ink,bodyX-torsoWidth/2-1,23,torsoWidth+2,18);
            Box(Shade(jersey,.6f),bodyX-torsoWidth/2,24,torsoWidth,16);
            Box(jersey,bodyX-torsoWidth/2+1,24,torsoWidth-4,13);
            Box(Shade(jersey,.85f),bodyX-torsoWidth/2+2,36,torsoWidth-3,4);
            Box(pants,bodyX-torsoWidth/2+1,40,torsoWidth-2,3);
            // Collar, pad highlights and trouser seams add depth without changing hit/ball anchors.
            Box(Shade(secondary,.75f),bodyX-5,23,10,2); Box(ink,bodyX-2,24,4,2);
            Box(new Color(Math.Min(1,jersey.Red+.16f),Math.Min(1,jersey.Green+.16f),Math.Min(1,jersey.Blue+.16f)),bodyX-torsoWidth/2+2,25,3,9);
            Box(Shade(pants,.65f),bodyX-torsoWidth/2+2,40,2,4);
            Box(ink,bodyX-7,40,14,1); Box(Colors.White,bodyX-1,40,3,2);
            // Shoulder pads and bent arms are posed in screen space for eight facings.
            for (var arm=0; arm<2; arm++)
            {
                var side=arm==0?-1:1;
                var shoulderX=bodyX+sx*side*10; var shoulderY=26+sy*side*6;
                var swing=Blocking?4:stride*-side*.6;
                var elbowX=shoulderX+sx*side*4+fx*swing;
                var elbowY=34+sy*side*5+fy*swing*.5;
                var handX=elbowX+fx*4; var handY=elbowY-2;
                if (Blocking) { elbowX=shoulderX+fx*8; elbowY=shoulderY+fy*5+3; handX=elbowX+fx*5; handY=elbowY+fy*3; }
                if (Passing && side>0) { elbowX=bodyX-fx*9; elbowY=20; handX=elbowX-fx*3; handY=10; }
                OutlinedLimb(jersey,shoulderX,shoulderY,elbowX,elbowY,8);
                OutlinedLimb(skinColor,elbowX,elbowY,handX,handY,5);
                Oval(ink,(int)handX-3,(int)handY-3,7,7); Oval(Colors.White,(int)handX-2,(int)handY-2,5,5);
                Box(primary,shoulderX-3,shoulderY-2,6,2); Box(secondary,shoulderX-3,shoulderY+1,6,1);
            }
            // Helmet shading, crown stripe, earhole and facemask replace the flat prototype crown.
            var headX=(int)Math.Round(bodyX+fx*2); var headY=15;
            canvas.SaveState(); canvas.Translate(headX,headY); canvas.Scale(.75f,.75f); canvas.Translate(-headX,-headY);
            if(front) { Oval(ink,headX-7,headY+1,15,14); Oval(skinColor,headX-6,headY+2,13,11); }
            Oval(ink,headX-12,headY-12,25,23);
            Oval(Shade(primary,.55f),headX-11,headY-11,23,20);
            Oval(primary,headX-10,headY-11,20,17);
            Oval(new Color(Math.Min(1,primary.Red+.23f),Math.Min(1,primary.Green+.23f),Math.Min(1,primary.Blue+.23f)),headX-8,headY-10,10,5);
            Box(secondary,headX-2-fx*3,headY-11,4,17);
            if(Math.Abs(fx)>.4)
            {
                var faceX=headX+fx*10;
                Box(ink,faceX-2,headY-1,5,6); Box(skinColor,faceX-1,headY+1,4,4);
                Box(Color.FromArgb("#D6DDE4"),faceX+fx*4-1,headY,2,10);
                Box(Color.FromArgb("#D6DDE4"),Math.Min(faceX,faceX+fx*5),headY+7,6,2);
                Box(ink,headX-fx*5,headY+1,3,3);
            }
            else if(front)
            {
                Box(ink,headX-7,headY+3,15,3);
                Box(Color.FromArgb("#D6DDE4"),headX-8,headY+5,2,8); Box(Color.FromArgb("#D6DDE4"),headX+7,headY+5,2,8);
                Box(Color.FromArgb("#D6DDE4"),headX-7,headY+10,15,2);
            }
            canvas.RestoreState();
            // Back/chest number remains readable; the art changes facing rather than rotating text.
            canvas.FontColor=away?primary:Colors.White; canvas.Font=Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize=Label.Length>2?10:13;
            canvas.DrawString(Label,(float)bodyX-12,27,24,13,HorizontalAlignment.Center,VerticalAlignment.Center);
            canvas.RestoreState();
        }
    }
}
