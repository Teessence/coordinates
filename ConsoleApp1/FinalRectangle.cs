namespace ConsoleApp1
{
    class FinalRectangle
    {
        public string Text { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public List<FinalRectangle> Associated { get; set; }

        public FinalRectangle(string text, double x, double y, double width, double height)
        {
            Text = text;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Associated = new List<FinalRectangle>();
        }
    }
}
