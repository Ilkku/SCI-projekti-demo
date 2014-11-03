using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Error
{
    public class Screen
    {
        /* OHJE:
         * Jokaisessa ruudussa, jossa halutaan scrollata pit�� m��ritt�� toi
         * MaxOffset. N�iden arvoksi tulee sitten ruudun korkeus + niin monta pikseli�
         * kuin haluaa pysty� kelaamaan ruutua alas.
         */
        // TODO: scrollauksen pys�ytys n�ytt�� koskettamalla
        public string Name = null;
        public float MaxOffset = 0;
        public float Offset = 0;
        public float Velocity = 0;
        public Dictionary<string, Button> Buttons;
        static int scrollbarWidth = 5;
        Rectangle scrollbarArea = new Rectangle(App.screenWidth - scrollbarWidth, 0, scrollbarWidth, 10);

        public Screen(string name)
        {
            Name = name;
            Buttons = new Dictionary<string, Button>();
        }
        public void Add(params Button[] buttons)
        {
            foreach (var btn in buttons)
            {
                Buttons.Add(btn.Name, btn);
            }
        }
        public virtual void Update(GestureSample gesture)
        {
            switch (gesture.GestureType)
            {
                case GestureType.Tap:
                    foreach (var btn in Buttons.Values)
                    {
                        if (btn.Visible && btn.TouchArea.Contains(gesture.Position) && btn.Click != null)
                        {
                            btn.Click();
                            return;
                        }
                    }
                    // pys�yt� scrollaus
                    //TODO: pys�ytys my�s kosketuksella ilman sormen yl�s nostamista
                    Velocity = 0;
                    break;
            }
        }
        public virtual void Draw()
        {
            App.SpriteBatch.Begin();
            foreach (var btn in Buttons.Values) btn.Draw();
            // piirret��n scrollbar jos on scrollattu
            if (Offset < 0)    // positiivinen offset tarkoittaisi scrollausta ruudun yl�puolelle
            {
                // optimointi puuttuu: scrollbarSize muuttuu vain ruutua vaihtaessa tai uutta hakua tehdess�
                int scrollbarSize = (int) (App.screenHeight * App.screenHeight / (App.screenHeight + MaxOffset));
                int scrollbarPos = (int)(Offset / MaxOffset * (App.screenHeight - scrollbarSize));
                scrollbarArea.Height = scrollbarSize;
                scrollbarArea.Y = - scrollbarPos;

                App.SpriteBatch.Draw(App.Pixel, scrollbarArea, Color.DarkSlateGray);
            }
            App.SpriteBatch.End();
        }
    }
    public class CollectingScreen : Screen
    {
        public CollectingScreen() : base("Collecting") { }
        public override void Draw()
        {
            App.SpriteBatch.Begin();
            Order order = App.Instance.CollectingData.CurrentOrder;
            if (App.Instance.CollectingData.ShowOrderInfo && order != null)
            {
                App.SpriteBatch.DrawStringCentered(App.Font, order.Customer, new Rectangle(0, 0, 240, 50), Color.Black, 0.5f);
                App.SpriteBatch.DrawStringCentered(App.Font, order.RequestedShippingDate.ToString(), new Rectangle(240, 0, 240, 50), Color.Black, 0.5f);
            }
            OrderLine line = App.Instance.CollectingData.CurrentLine;
            Product product = App.Instance.CollectingData.CurrentProduct;
            if (App.Instance.CollectingData.ShowLineInfo && line != null && product != null)
            {
                App.SpriteBatch.DrawStringCentered(App.Font,
                    "tuote " + (App.Instance.CollectingData.CurrentLineIndex + 1) + "/" + order.Lines.Count,
                    new Rectangle(0, 90, 480, 50), Color.Black, 0.6f);
                App.SpriteBatch.DrawStringCentered(App.Font, product.Description, new Rectangle(40, 100, 400, 100), Color.Black, 1f);
                App.SpriteBatch.DrawStringCentered(App.Font, line.Amount + " kpl, " + line.Amount / product.PackageSize + " pakettia", new Rectangle(40, 200, 400, 100), Color.Black, 1f);
                App.SpriteBatch.DrawStringCentered(App.Font, "Tuotekoodi: " + product.Code, new Rectangle(40, 300, 400, 100), Color.Black, 1f);
                App.SpriteBatch.DrawStringCentered(App.Font, "Hyllypaikka: " + product.ShelfCode, new Rectangle(40, 400, 400, 100), Color.Black, 1f);
            }
            App.SpriteBatch.End();
            base.Draw();
        }
    }
    public class StartScreen : Screen
    {
        public StartScreen() : base("Start") { }
        public override void Draw()
        {
            App.SpriteBatch.Begin();
            App.SpriteBatch.DrawStringCentered(App.Font, "Error", new Rectangle(0, 0, 480, 120), Color.Black, 1f);
            if (App.Message != null)
            {
                App.SpriteBatch.DrawStringCentered(App.Font, App.Message, new Rectangle(50, 200, 380, 60), Color.Black, 0.75f);
            }
            App.SpriteBatch.End();
            base.Draw();
        }
    }
    public class SearchScreen : Screen
    {
        public List<string> SearchResult;

        public SearchScreen() : base("Search") { }

        public override void Draw()
        {
            App.SpriteBatch.Begin(SpriteSortMode.Texture, null, null, null, null, null, Matrix.CreateTranslation(0, Offset, 0));
            var v = new Vector2(10, 100);
            if (SearchResult != null)
            {
                // tekstit alkavat korkeudelta y=100
                int _maxOffsetY = 100;
                foreach (var textLine in SearchResult)
                {
                    App.SpriteBatch.DrawString(App.Font, textLine, v, Color.DarkSlateGray, 0.6f, 0f);
                    v.Y += 50;
                    // lis�� MaxOffset:n arvoa jokaisen kirjoitetun rivin verran
                    _maxOffsetY += 50;
                }
                if (_maxOffsetY > App.screenHeight)
                {
                    MaxOffset = _maxOffsetY - App.screenHeight;
                }
                Offset -= Velocity * (float) App.Instance.TargetElapsedTime.TotalSeconds;
                // est�� scrollauksen ruudun yl�puolelle
                if (Offset > 0)
                {
                    Offset = 0;
                }
                // est�� scrollauksen liian alas
                else if (Offset < -MaxOffset)
                {
                    Offset = -MaxOffset;
                }
                if (Offset == 0 || Offset == -MaxOffset)
                {
                    Velocity = 0;
                }
                // v�hennet��n nopeutta pikkuhiljaa (kitkaefekti)
                if (Velocity > 0)
                {
                    Velocity -= 100;    // kitkan suuruus
                    if (Velocity < 0)
                    {
                        Velocity = 0;
                    }
                }
                else if (Velocity < 0)
                {
                    Velocity += 100;
                    if (Velocity > 0)
                    {
                        Velocity = 0;
                    }
                }
            }
            App.SpriteBatch.End();
            base.Draw();
        }
        public override void Update(GestureSample gesture)
        {
            switch (gesture.GestureType)
            {
                // FreeDragin kanssa ei tarvi olla niin tarkkana suunnan kanssa.
                // Lis�ks kun vaan yksi drag-tyyppinen gesture on enabled,
                // niin scrollaaminen ei p�tki kun GestureType ei koko ajan vaihtele vertical ja free v�lill�
                case GestureType.FreeDrag:
                    // move the search screen vertically by the drag delta
                    // amount.
                    Offset += gesture.Delta.Y;
                    break;
                case GestureType.Flick:
                    // add velocity to screen (only interested in changes to Y velocity).
                    Velocity -= gesture.Delta.Y;
                    break;
            }
            base.Update(gesture);
        }
    }
    public class MapScreen : Screen
    {
        public MapScreen() : base("Map") { }
        public override void Draw()
        {
            App.Instance.DrawMapScreen();
            base.Draw();
        }
    }    
}
