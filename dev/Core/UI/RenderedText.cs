﻿/***************************************************************************
 *   RenderedText.cs
 *   
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 3 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/
#region usings
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UltimaXNA.Core.Graphics;
using UltimaXNA.Core.UI.HTML;
#endregion

namespace UltimaXNA.Core.UI
{
    /// <summary>
    /// A texture containing rendered text. Can interpret html. Will automatically update.
    /// </summary>
    class RenderedText
    {
        const int DefaultRenderedTextWidth = 200;

        string m_Text;
        HtmlDocument m_Document;
        bool m_MustRender;
        bool m_CollapseContent;
        int m_MaxWidth;

        public string Text
        {
            get { return m_Text; }
            set
            {
                if (m_Text != value)
                {
                    m_MustRender = true;
                    m_Text = value;
                }
            }
        }

        public int MaxWidth
        {
            get { return m_MaxWidth; }
            set
            {
                if (value <= 0)
                    value = DefaultRenderedTextWidth;
                if (m_MaxWidth != value)
                {
                    m_MustRender = true;
                    m_MaxWidth = value;
                }
            }
        }

        public int Width
        {
            get
            {
                if (Text == null)
                    return 0;
                RenderIfNecessary();
                return m_Document.Width;
            }
        }

        public int Height
        {
            get
            {
                if (Text == null)
                    return 0;
                RenderIfNecessary();
                return m_Document.Height;
            }
        }

        public int MouseOverRegionID
        {
            get;
            set;
        }

        public bool IsMouseDown
        {
            get;
            set;
        }

        public HtmlLinkList Regions
        {
            get { return m_Document.Links; }
        }

        public Texture2D Texture
        {
            get
            {
                RenderIfNecessary();
                return m_Document.Texture;
            }
        }

        public RenderedText(string text, int maxWidth = DefaultRenderedTextWidth, bool collapseContent = false)
        {
            Text = text;
            MaxWidth = maxWidth;
            m_CollapseContent = collapseContent;
            m_MustRender = true;
        }

        // ============================================================================================================
        // Draw methods
        // ============================================================================================================

        public void Draw(SpriteBatchUI sb, Point position, Vector3? hueVector = null)
        {
            Draw(sb, new Rectangle(position.X, position.Y, Width, Height), 0, 0, hueVector);
        }

        public void Draw(SpriteBatchUI sb, Rectangle destRectangle, int xScroll, int yScroll, Vector3? hueVector = null)
        {
            if (Text == null)
            {
                return;
            }
            Rectangle sourceRectangle;
            if ((xScroll > Width) || (xScroll < -MaxWidth) || (yScroll > Height) || (yScroll < -Height))
            {
                return;
            }
            sourceRectangle.X = xScroll;
            sourceRectangle.Y = yScroll;
            int maxX = sourceRectangle.X + destRectangle.Width;
            if (maxX <= Width)
            {
                sourceRectangle.Width = destRectangle.Width;
            }
            else
            {
                sourceRectangle.Width = Width - sourceRectangle.X;
                destRectangle.Width = sourceRectangle.Width;
            }
            int maxY = sourceRectangle.Y + destRectangle.Height;
            if (maxY <= Height)
            {
                sourceRectangle.Height = destRectangle.Height;
            }
            else
            {
                sourceRectangle.Height = Height - sourceRectangle.Y;
                destRectangle.Height = sourceRectangle.Height;
            }
            sb.Draw2D(m_Document.Texture, destRectangle, sourceRectangle, hueVector.HasValue ? hueVector.Value : Vector3.Zero);
            for (int i = 0; i < m_Document.Links.Count; i++)
            {
                HtmlLink link = m_Document.Links[i];
                Point pos;
                Rectangle srcRect;
                if (ClipRectangle(new Point(xScroll, yScroll), link.Area, destRectangle, out pos, out srcRect))
                {
                    // only draw the font in a different color if this is a HREF region.
                    // otherwise it is a dummy region used to notify images that they are
                    // being mouse overed.
                    if (link.HREF != null)
                    {
                        int linkHue = 0;
                        if (link.Index == MouseOverRegionID)
                        {
                            if (IsMouseDown)
                            {
                                linkHue = link.Style.ActiveColorHue;
                            }
                            else
                            {
                                linkHue = link.Style.HoverColorHue;
                            }
                        }
                        else
                        {
                            linkHue = link.Style.ColorHue;
                        }
                        sb.Draw2D(m_Document.Texture, new Vector3(pos.X, pos.Y, 0), srcRect, Utility.GetHueVector(linkHue));
                    }
                }
            }

            for (int i = 0; i < m_Document.Images.Count; i++)
            {
                HtmlImage image = m_Document.Images[i];
                Point position;
                Rectangle srcRect;
                if (ClipRectangle(new Point(xScroll, yScroll), image.Area, destRectangle, out position, out srcRect))
                {
                    Rectangle srcImage = new Rectangle(
                        srcRect.X - image.Area.X, srcRect.Y - image.Area.Y,
                        srcRect.Width, srcRect.Height);
                    Texture2D texture = null;

                    // is the mouse over this image?
                    if (image.LinkIndex != -1 && image.LinkIndex == MouseOverRegionID)
                    {
                        if (IsMouseDown)
                        {
                            texture = image.TextureDown;
                        }
                        if (texture == null)
                        {
                            texture = image.TextureOver;
                        }
                        if (texture == null)
                        {
                            texture = image.Texture;
                        }
                    }
                    if (texture == null)
                    {
                        texture = image.Texture;
                    }
                    if (srcImage.Width > texture.Width)
                    {
                        srcImage.Width = texture.Width;
                    }
                    if (srcImage.Height > texture.Height)
                    {
                        srcImage.Height = texture.Height;
                    }
                    sb.Draw2D(texture, new Vector3(position.X, position.Y, 0),
                        srcImage, Utility.GetHueVector(0, false, false, true));
                }
            }
        }

        bool ClipRectangle(Point offset, Rectangle srcRect, Rectangle clipTo, out Point posClipped, out Rectangle srcClipped)
        {
            posClipped = new Point(clipTo.X + srcRect.X - offset.X, clipTo.Y + srcRect.Y - offset.Y);
            srcClipped = srcRect;
            Rectangle dstClipped = srcRect;
            dstClipped.X += clipTo.X - offset.X;
            dstClipped.Y += clipTo.Y - offset.Y;
            if (dstClipped.Bottom < clipTo.Top)
            {
                return false;
            }
            if (dstClipped.Top < clipTo.Top)
            {
                srcClipped.Y += (clipTo.Top - dstClipped.Top);
                srcClipped.Height -= (clipTo.Top - dstClipped.Top);
                posClipped.Y += (clipTo.Top - dstClipped.Top);
            }
            if (dstClipped.Top > clipTo.Bottom)
            {
                return false;
            }
            if (dstClipped.Bottom > clipTo.Bottom)
                srcClipped.Height += (clipTo.Bottom - dstClipped.Bottom);

            if (dstClipped.Right < clipTo.Left)
            {
                return false;
            }
            if (dstClipped.Left < clipTo.Left)
            {
                srcClipped.X += (clipTo.Left - dstClipped.Left);
                srcClipped.Width -= (clipTo.Left - dstClipped.Left);
                posClipped.X += (clipTo.Left - dstClipped.Left);
            }
            if (dstClipped.Left > clipTo.Right)
            {
                return false;
            }
            if (dstClipped.Right > clipTo.Right)
            {
                srcClipped.Width += (clipTo.Right - dstClipped.Right);
            }
            return true;
        }

        void RenderIfNecessary()
        {
            if (m_Document == null || m_MustRender)
            {
                if (m_Document == null)
                {
                    m_Document = new HtmlDocument(Text, MaxWidth, m_CollapseContent);
                }
                else
                {
                    m_Document.Reset();
                    m_Document.SetHtml(Text, MaxWidth);
                }
                m_Document.Render();
                m_MustRender = false;
            }
        }
    }
}
