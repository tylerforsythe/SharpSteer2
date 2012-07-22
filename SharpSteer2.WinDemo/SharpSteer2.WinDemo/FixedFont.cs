using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bnoerj.AI.Steering
{
    /// <summary>
    /// The main runtime font class. These objects are loaded from XNB format
    /// using the ContentManager, and contain all the information needed to
    /// render text to the screen.
    /// </summary>
    public class FixedFont
    {
        // A single texture contains all the characters of the font.
        Texture2D textureValue;
		Vector2 size;

		public Vector2 Size
		{
			get { return size; }
		}

		public FixedFont(Texture2D texture)
        {
			size = new Vector2(8, 15);
            textureValue = texture;
        }

        /// <summary>
        /// Draws text to the screen.
        /// </summary>
        public void Draw(String text, Vector2 position, float scale, Color color, SpriteBatch spriteBatch)
        {
			float left = position.X;
            foreach (Char ch in text)
            {
                if (ch == 0x0A)
				{
					// New line
					position.X = left;
					position.Y += size.Y;
					continue;
				}
				else if (ch > 0x20 && ch <= 0x7E)
				{
					int index = ch - 0x21;

					// Look up what part of the texture represents this character
					// 16 glyphs per row
					int x = (index % 16) * (int)size.X;
					int y = index / 16 * (int)size.Y;

					Rectangle glyph = new Rectangle(x, y, (int)size.X, (int)size.Y);
					Vector2 pos = new Vector2((int)position.X, (int)position.Y);
					spriteBatch.Draw(textureValue, pos, glyph, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
				}

				position.X += (int)(size.X * scale);
            }
        }
    }
}
