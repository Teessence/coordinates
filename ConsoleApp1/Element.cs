namespace ConsoleApp1
{
    class Element
    {
        public string Text { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMerged { get; set; }
        public List<Rectangle> ElementPositions { get; set; }
        public Rectangle CombinedRectangle { get; set; }
        public List<Element> IntersectingRectangles { get; set; }

        public Element(string text, double x, double y, double width, double height, bool isMerged, List<Rectangle> elementPositions, Rectangle combinedRectangle)
        {
            Text = text;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            IsMerged = isMerged;
            ElementPositions = elementPositions;
            CombinedRectangle = combinedRectangle;
            IntersectingRectangles = new List<Element>();
        }
    }
}
