using OpenTK;
using OpenTK.Graphics;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding;
using System;

namespace StorybrewScripts
{
    public class Tetris : StoryboardObjectGenerator
    {
        [Configurable]
        public int StartTime = 0;

        [Configurable]
        public int EndTime = 0;

        [Configurable]
        public double BeatDivisor = 1;

        [Configurable]
        public string SpritePath = "sb/sq.png";

        [Configurable]
        public double SpriteScale = 0.625;

        [Configurable]
        public Vector2 Offset = new Vector2(320, 240);

        [Configurable]
        public Vector2 ShadowOffset = new Vector2(4, 4);

        [Configurable]
        public double Rotation = 0;

        [Configurable]
        public int GridWidth = 10;

        [Configurable]
        public int GridHeight = 20;

        [Configurable]
        public float CellSize = 20;

        [Configurable]
        public int BlockLength = 4;

        [Configurable]
        public int Blocks = 1;

        [Configurable]
        public bool Wait = true;

        [Configurable]
        public bool Dumb = false;

        [Configurable]
        public Color4 Color;

        public class Cell
        {
            public int X;
            public int Y;

            public OsbSprite Sprite;
            public OsbSprite Shadow;

            public bool HasSprite { get { return Sprite != null; } }
        }
        private Cell[,] cells;

        public override void Generate()
        {
            var beatDuration = Beatmap.GetTimingPointAt(0).BeatDuration;
            var timestep = beatDuration / BeatDivisor;

            cells = new Cell[GridWidth, GridHeight];
            for (var x = 0; x < GridWidth; x++)
                for (var y = 0; y < GridHeight; y++)
                    cells[x, y] = new Cell() { X = x, Y = y, };
            for (var time = (double)StartTime; time < EndTime; time += timestep)
            {
                for (var i = 0; i < Blocks; i++)
                    addBlock(time - timestep, time);
                if (clearLines(time, time + timestep))
                    time += Wait ? timestep : 0;
            }

            for (var x = 0; x < GridWidth; x++)
                for (var y = 0; y < GridHeight; y++)
                    if (cells[x, y].HasSprite)
                        killCell(EndTime, EndTime + timestep, x, y);
        }

        private void addBlock(double startTime, double endTime)
        {
            var brightness = (float)Random(0.3, 1.0);
            var color = new Color4(Color.R * brightness, Color.G * brightness, Color.B * brightness, 1);

            var heightMap = new int[GridWidth];
            var bottom = 0;
            for (var x = 0; x < GridWidth; x++)
            {
                for (var y = 0; y < GridHeight; y++)
                {
                    if (cells[x, y].HasSprite)
                        break;

                    heightMap[x] = y;
                }
                bottom = Math.Max(bottom, heightMap[x]);
            }

            var dropX = Random(GridWidth);
            while (!Dumb && heightMap[dropX] != bottom)
                dropX = Random(GridWidth);

            var dropY = heightMap[dropX];

            fillCell(startTime, endTime, dropX, dropY, color);
            for (var i = 1; i < BlockLength; i++)
            {
                var options = new int[] { 0, 1, 2, 3 };
                shuffle(options);

                foreach (var option in options)
                {
                    var nextDropX = dropX;
                    var nextDropY = dropY;

                    switch (option)
                    {
                        case 0: nextDropX++; break;
                        case 1: nextDropY++; break;
                        case 2: nextDropX--; break;
                        case 3: nextDropY--; break;
                    }

                    if (nextDropX < 0 || nextDropX >= GridWidth || nextDropY < 0 || nextDropY >= GridHeight)
                        continue;

                    if (cells[nextDropX, nextDropY].HasSprite)
                        continue;

                    if (heightMap[nextDropX] < nextDropY)
                        continue;

                    dropX = nextDropX;
                    dropY = nextDropY;
                    fillCell(startTime, endTime, dropX, dropY, color);
                    break;
                }
            }
        }

        private bool clearLines(double startTime, double endTime)
        {
            var anyCombo = false;
            var dropHeight = 0;
            for (var y = GridHeight - 1; y >= 0; y--)
            {
                var combo = true;
                for (var x = 0; x < GridWidth; x++)
                    if (!cells[x, y].HasSprite)
                    {
                        combo = false;
                        break;
                    }

                if (combo)
                {
                    anyCombo = true;
                    for (var x = 0; x < GridWidth; x++)
                        killCell(startTime, endTime, x, y);

                    dropHeight++;
                }
                else if (dropHeight > 0)
                {
                    for (var x = 0; x < GridWidth; x++)
                        if (cells[x, y].HasSprite)
                            dropCell(startTime, endTime, x, y, dropHeight);
                }
            }
            return anyCombo;
        }

        private void fillCell(double startTime, double endTime, int dropX, int dropY, Color4 color)
        {
            var shadow = GetLayer("Shadows").CreateSprite(SpritePath, OsbOrigin.TopCentre);
            var sprite = GetLayer("Blocks").CreateSprite(SpritePath, OsbOrigin.TopCentre);

            cells[dropX, dropY].Sprite = sprite;
            cells[dropX, dropY].Shadow = shadow;

            var targetPosition = new Vector2(dropX * CellSize, dropY * CellSize);
            var startPosition = new Vector2(targetPosition.X, targetPosition.Y - CellSize * GridHeight);

            sprite.Rotate(startTime, Rotation / 180 * Math.PI);
            sprite.Scale(startTime, SpriteScale);
            sprite.Color(startTime, color);
            sprite.Move(OsbEasing.In, startTime, endTime, transform(startPosition), transform(targetPosition));

            shadow.Rotate(startTime, Rotation / 180 * Math.PI);
            shadow.Scale(startTime, SpriteScale);
            shadow.Color(startTime, 0, 0, 0);
            shadow.Fade(startTime, 0.5);
            shadow.Move(OsbEasing.In, startTime, endTime, transform(startPosition) + ShadowOffset, transform(targetPosition) + ShadowOffset);
        }

        private void killCell(double startTime, double endTime, int dropX, int dropY)
        {
            var sprite = cells[dropX, dropY].Sprite;
            var shadow = cells[dropX, dropY].Shadow;
            cells[dropX, dropY].Sprite = null;
            cells[dropX, dropY].Shadow = null;

            sprite.Scale(startTime, endTime, SpriteScale, 0);
            sprite.Color(startTime, Color);

            shadow.Scale(startTime, endTime, SpriteScale, 0);
        }

        private void dropCell(double startTime, double endTime, int dropX, int dropY, int dropHeight)
        {
            var sprite = cells[dropX, dropY].Sprite;
            var shadow = cells[dropX, dropY].Shadow;

            cells[dropX, dropY].Sprite = null;
            cells[dropX, dropY + dropHeight].Sprite = sprite;

            cells[dropX, dropY].Shadow = null;
            cells[dropX, dropY + dropHeight].Shadow = shadow;

            var targetPosition = new Vector2(dropX * CellSize, (dropY + dropHeight) * CellSize);
            var startPosition = new Vector2(targetPosition.X, dropY * CellSize);

            sprite.Move(OsbEasing.In, startTime, endTime, transform(startPosition), transform(targetPosition));
            shadow.Move(OsbEasing.In, startTime, endTime, transform(startPosition) + ShadowOffset, transform(targetPosition) + ShadowOffset);
        }

        private Vector2 transform(Vector2 position)
        {
            position = new Vector2(position.X - GridWidth * CellSize * 0.5f, position.Y - GridHeight * CellSize);
            return Vector2.Transform(position, Quaternion.FromEulerAngles((float)(Rotation / 180 * Math.PI), 0, 0)) + Offset;
        }

        private void shuffle(int[] array)
        {
            var n = array.Length;
            while (n > 1)
            {
                n--;
                var k = Random(n + 1);
                var value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
        }
    }
}
