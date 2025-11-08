using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

using static Celeste.Mod.PandorasBox.MarioClearPipeHelper;

namespace Celeste.Mod.PandorasBox
{
    [Tracked(true)]
    public class MarioClearPipeSolid : Solid
    {
        private Vector2 startNode;
        private Vector2 endNode;
        private Vector2 nextNode;

        private bool startNodeExit;
        private bool endNodeExit;

        private float length;
        private float pipeWidth;

        private string texturePath;
        private MTexture pipeTexture;

        public static Dictionary<string, Dictionary<string, Vector2>> ExitTexturesQuads = new Dictionary<string, Dictionary<string, Vector2>>()
        {
            {
                "up", new Dictionary<string, Vector2>() {
                    {"left", new Vector2(0, 3)},
                    {"middle", new Vector2(1, 3)},
                    {"right", new Vector2(2, 3)},
                }
            },
            {
                "down", new Dictionary<string, Vector2>() {
                    {"left", new Vector2(0, 5)},
                    {"middle", new Vector2(1, 5)},
                    {"right", new Vector2(2, 5)},
                }
            },
            {
                "left", new Dictionary<string, Vector2>() {
                    {"top", new Vector2(0, 0)},
                    {"middle", new Vector2(0, 1)},
                    {"bottom", new Vector2(0, 2)},
                }
            },
            {
                "right", new Dictionary<string, Vector2>() {
                    {"top", new Vector2(2, 0)},
                    {"middle", new Vector2(2, 1)},
                    {"bottom", new Vector2(2, 2)},
                }
            },
        };

        public static Dictionary<string, Dictionary<string, Vector2>> StraightTextureQuads = new Dictionary<string, Dictionary<string, Vector2>>()
        {
            {
                "vertical", new Dictionary<string, Vector2>() {
                    {"left", new Vector2(0, 4)},
                    {"middle", new Vector2(1, 4)},
                    {"right", new Vector2(2, 4)},
                }
            },
            {
                "horizontal", new Dictionary<string, Vector2>() {
                    {"top", new Vector2(1, 0)},
                    {"middle", new Vector2(1, 1)},
                    {"bottom", new Vector2(1, 2)},
                }
            },
        };

        public static Dictionary<string, Dictionary<string, Vector2>> CornerTextureQuads = new Dictionary<string, Dictionary<string, Vector2>>()
        {
            {
                "upLeft", new Dictionary<string, Vector2>() {
                    {"inner", new Vector2(3, 0)},
                    {"outer", new Vector2(5, 2)},
                    {"horizontal_middle", new Vector2(3, 1)},
                    {"horizontal_wall", new Vector2(3, 2)},
                    {"vertical_middle", new Vector2(4, 0)},
                    {"vertical_wall", new Vector2(5, 0)},
                }
            },
            {
                "upRight", new Dictionary<string, Vector2>() {
                    {"inner", new Vector2(8, 0)},
                    {"outer", new Vector2(6, 2)},
                    {"horizontal_middle", new Vector2(8, 1)},
                    {"horizontal_wall", new Vector2(8, 2)},
                    {"vertical_middle", new Vector2(7, 0)},
                    {"vertical_wall", new Vector2(6, 0)},
                }
            },
            {
                "downRight", new Dictionary<string, Vector2>() {
                    {"inner", new Vector2(8, 5)},
                    {"outer", new Vector2(6, 3)},
                    {"horizontal_middle", new Vector2(8, 4)},
                    {"horizontal_wall", new Vector2(8, 3)},
                    {"vertical_middle", new Vector2(7, 5)},
                    {"vertical_wall", new Vector2(6, 5)},
                }
            },
            {
                "downLeft", new Dictionary<string, Vector2>() {
                    {"inner", new Vector2(3, 5)},
                    {"outer", new Vector2(5, 3)},
                    {"horizontal_middle", new Vector2(3, 4)},
                    {"horizontal_wall", new Vector2(3, 3)},
                    {"vertical_middle", new Vector2(4, 5)},
                    {"vertical_wall", new Vector2(5, 5)},
                }
            },
        };

        public static MTexture GetTextureQuad(MTexture texture, Vector2 position)
        {
            return texture.GetSubtexture((int)position.X * 8, (int)position.Y * 8, 8, 8);
        }

        public MarioClearPipeSolid(Vector2 position, float width, float height, float length, float pipeWidth, string texturePath, int surfaceSound, Vector2 startNode, Vector2 endNode, Vector2 nextNode, bool startNodeExit, bool endNodeExit) : base(position, width, height, true)
        {
            this.startNode = startNode;
            this.endNode = endNode;
            this.nextNode = nextNode;

            this.startNodeExit = startNodeExit;
            this.endNodeExit = endNodeExit;

            this.length = length;
            this.pipeWidth = pipeWidth;

            this.texturePath = texturePath;

            pipeTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/pipe"];

            Depth = -11000;
            SurfaceSoundIndex = surfaceSound >= 0 ? surfaceSound : 11; // 11 = Deactivated Space Jam, 

            addPipeVisuals();

            EnableAssistModeChecks = false;
        }

        public static MarioClearPipeSolid FromNodes(Vector2 startNode, Vector2 endNode, Vector2 nextNode, bool startNodeExit, bool endNodeExit, float pipeWidth, string texturePath, int surfaceSound)
        {
            Direction direction = GetPipeExitDirection(endNode, startNode);

            float length = (endNode - startNode).Length();

            float halfPipeWidth = pipeWidth / 2f;

            Vector2 widthVector = Vector2.Zero;
            Vector2 lengthVector = Vector2.Zero;

            Vector2 position = startNode;
            float width = 8f;
            float height = 8f;

            if (startNodeExit)
            {
                length += halfPipeWidth;
            }

            if (endNodeExit)
            {
                length -= halfPipeWidth;
            }

            switch (direction)
            {
                case Direction.Up:
                    position = new Vector2(endNode.X - halfPipeWidth, endNode.Y - (endNodeExit ? 0 : halfPipeWidth));
                    width = pipeWidth;
                    height = length;
                    break;

                case Direction.Right:
                    position = new Vector2(startNode.X + (startNodeExit ? 0 : halfPipeWidth), startNode.Y - halfPipeWidth);
                    width = length;
                    height = pipeWidth;
                    break;

                case Direction.Down:
                    position = new Vector2(startNode.X - halfPipeWidth, startNode.Y + (startNodeExit ? 0 : halfPipeWidth));
                    width = pipeWidth;
                    height = length;
                    break;

                case Direction.Left:
                    position = new Vector2(endNode.X - (endNodeExit ? 0 : halfPipeWidth), endNode.Y - halfPipeWidth);
                    width = length;
                    height = pipeWidth;
                    break;

                default:
                    break;
            }

            // Weird offset on non multiple of 16 widths (8, 24, etc)
            if (pipeWidth / 8 % 2 == 1)
            {
                if (endNodeExit && direction == Direction.Right || startNodeExit && direction == Direction.Left)
                {
                    width -= endNodeExit && startNodeExit ? 0f : 4f;
                    length -= endNodeExit && startNodeExit ? 0f : 4f;
                }
                else if (startNodeExit && direction == Direction.Up || endNodeExit && direction == Direction.Down)
                {
                    height -= endNodeExit && startNodeExit ? 0f : 4f;
                    length -= endNodeExit && startNodeExit ? 0f : 4f;
                }
            }

            return new MarioClearPipeSolid(position, width, height, length, pipeWidth, texturePath, surfaceSound, startNode, endNode, nextNode, startNodeExit, endNodeExit);
        }

        private static string getCornerType(Direction direction, Direction nextDirection)
        {
            if (direction == Direction.Right && nextDirection == Direction.Up ||
                direction == Direction.Down && nextDirection == Direction.Left)
            {
                return "upLeft";
            } 
            else if (direction == Direction.Down && nextDirection == Direction.Right ||
                direction == Direction.Left && nextDirection == Direction.Up)
            {
                return "upRight";
            }
            else if (direction == Direction.Left && nextDirection == Direction.Down ||
                direction == Direction.Up && nextDirection == Direction.Right)
            {
                return "downRight";
            }
            else if (direction == Direction.Up && nextDirection == Direction.Left ||
                direction == Direction.Right && nextDirection == Direction.Down)
            {
                return "downLeft";
            }
            else
            {
                return "unknown";
            }
        }

        private void addPipeCornerVisuals(Direction direction, Direction nextDirection)
        {
            if (length < pipeWidth)
            {
                return;
            }

            string cornerType = getCornerType(direction, nextDirection);

            if (cornerType != "upLeft" && cornerType != "upRight" && cornerType != "downRight" && cornerType != "downLeft")
            {
                return;
            }

            MTexture nonWallTexture = GetTextureQuad(pipeTexture, StraightTextureQuads["horizontal"]["middle"]); // TODO - Better texture?
            MTexture innerCornerTexture = GetTextureQuad(pipeTexture, CornerTextureQuads[cornerType]["inner"]);
            MTexture outerCornerTexture = GetTextureQuad(pipeTexture, CornerTextureQuads[cornerType]["outer"]);
            MTexture verticalWallTexture = GetTextureQuad(pipeTexture, CornerTextureQuads[cornerType]["vertical_wall"]);
            MTexture horizontalWallTexture = GetTextureQuad(pipeTexture, CornerTextureQuads[cornerType]["horizontal_wall"]);

            float verticalWallX = -1;
            float horizontalWallY = -1;

            float offsetX = 0;
            float offsetY = 0;

            if (cornerType == "upLeft")
            {
                verticalWallX = pipeWidth - 8;
                horizontalWallY = pipeWidth - 8;
            }
            else if (cornerType == "upRight")
            {
                verticalWallX = 0;
                horizontalWallY = pipeWidth - 8;
            }
            else if (cornerType == "downRight")
            {
                verticalWallX = 0;
                horizontalWallY = 0;
            }
            else if (cornerType == "downLeft")
            {
                verticalWallX = pipeWidth - 8;
                horizontalWallY = 0;
            }

            if (direction == Direction.Right)
            {
                offsetX = length - pipeWidth;
            }
            else if (direction == Direction.Down)
            {
                offsetY = length - pipeWidth;
            }

            for (int x = 0; x < pipeWidth; x += 8)
            {
                for (int y = 0; y < pipeWidth; y += 8)
                {
                    Image image;

                    bool onVertical = x == verticalWallX;
                    bool onHorizontal = y == horizontalWallY;
                    bool innerCorner = x == Math.Abs(verticalWallX - pipeWidth + 8) && y == Math.Abs(horizontalWallY - pipeWidth + 8);

                    if (innerCorner)
                    {
                        image = new Image(innerCornerTexture);
                    }
                    else if (onVertical && onHorizontal)
                    {
                        image = new Image(outerCornerTexture);
                    }
                    else if (onVertical)
                    {
                        image = new Image(verticalWallTexture);
                    }
                    else if (onHorizontal)
                    {
                        image = new Image(horizontalWallTexture);
                    }
                    else
                    {
                        image = new Image(nonWallTexture);
                    }

                    image.X = x + offsetX;
                    image.Y = y + offsetY;

                    Add(image);
                }
            }
        }

        private void addPipeVisuals()
        {
            Direction direction = GetPipeExitDirection(endNode, startNode);
            Direction nextDirection = GetPipeExitDirection(nextNode, endNode);

            bool horizontalPipe = direction == Direction.Right || direction == Direction.Left;
            bool verticalPipe = direction == Direction.Down || direction == Direction.Up;

            float straightLength = !endNodeExit ? length - pipeWidth : length;
            float offset = !endNodeExit && (direction == Direction.Up || direction == Direction.Left) ? pipeWidth : 0;

            if (horizontalPipe)
            {
                for (int x = 0; x < straightLength; x += 8)
                {
                    string columnType = "section";

                    Image top;
                    Image bottom;
                    MTexture middleTexture;

                    if (direction == Direction.Left && x == straightLength - 8 && startNodeExit || direction == Direction.Right && x == straightLength - 8 && endNodeExit)
                    {
                        columnType = "rightExit";
                    }
                    else if (direction == Direction.Left && x == 0 && endNodeExit || direction == Direction.Right && x == 0 && startNodeExit)
                    {
                        columnType = "leftExit";
                    }

                    if (columnType == "rightExit")
                    {
                        top = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["right"]["top"]));
                        bottom = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["right"]["bottom"]));

                        middleTexture = GetTextureQuad(pipeTexture, ExitTexturesQuads["right"]["middle"]);
                    }
                    else if (columnType == "leftExit")
                    {
                        top = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["left"]["top"]));
                        bottom = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["left"]["bottom"]));

                        middleTexture = GetTextureQuad(pipeTexture, ExitTexturesQuads["left"]["middle"]);
                    }
                    else
                    {
                        top = new Image(GetTextureQuad(pipeTexture, StraightTextureQuads["horizontal"]["top"]));
                        bottom = new Image(GetTextureQuad(pipeTexture, StraightTextureQuads["horizontal"]["bottom"]));

                        middleTexture = GetTextureQuad(pipeTexture, StraightTextureQuads["horizontal"]["middle"]);
                    }

                    top.X = x + offset;
                    top.Y = 0;

                    bottom.X = x + offset;
                    bottom.Y = pipeWidth - 8;

                    Add(top);
                    Add(bottom);

                    for (int y = 8; y < pipeWidth - 8; y += 8)
                    {
                        Image middle = new Image(middleTexture);

                        middle.X = x + offset;
                        middle.Y = y;

                        Add(middle);
                    }
                }
            }
            else if (verticalPipe)
            {
                for (int y = 0; y < straightLength; y += 8)
                {
                    string rowType = "section";

                    Image left;
                    Image right;
                    MTexture middleTexture;

                    if (direction == Direction.Up && y == straightLength - 8 && startNodeExit || direction == Direction.Down && y == straightLength - 8 && endNodeExit)
                    {
                        rowType = "downExit";
                    }
                    else if (direction == Direction.Up && y == 0 && endNodeExit || direction == Direction.Down && y == 0 && startNodeExit)
                    {
                        rowType = "upExit";
                    }

                    if (rowType == "upExit")
                    {
                        left = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["up"]["left"]));
                        right = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["up"]["right"]));

                        middleTexture = GetTextureQuad(pipeTexture, ExitTexturesQuads["up"]["middle"]);
                    }
                    else if (rowType == "downExit")
                    {
                        left = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["down"]["left"]));
                        right = new Image(GetTextureQuad(pipeTexture, ExitTexturesQuads["down"]["right"]));

                        middleTexture = GetTextureQuad(pipeTexture, ExitTexturesQuads["down"]["middle"]);
                    }
                    else
                    {
                        left = new Image(GetTextureQuad(pipeTexture, StraightTextureQuads["vertical"]["left"]));
                        right = new Image(GetTextureQuad(pipeTexture, StraightTextureQuads["vertical"]["right"]));

                        middleTexture = GetTextureQuad(pipeTexture, StraightTextureQuads["vertical"]["middle"]);
                    }

                    left.X = 0;
                    left.Y = y + offset;

                    right.X = pipeWidth - 8;
                    right.Y = y + offset;

                    Add(left);
                    Add(right);

                    for (int x = 8; x < pipeWidth - 8; x += 8)
                    {
                        Image middle = new Image(middleTexture);

                        middle.X = x;
                        middle.Y = y + offset;

                        Add(middle);
                    }
                }
            }

            if (!endNodeExit)
            {
                addPipeCornerVisuals(direction, nextDirection);
            }
        }
    }
}
