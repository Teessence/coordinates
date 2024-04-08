using ConsoleApp1;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

JsonSerializerOptions jso = new JsonSerializerOptions();
jso.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
//jso.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;


var service = FirefoxDriverService.CreateDefaultService();
service.FirefoxBinaryPath = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\FireFox.lnk";

FirefoxDriver driver = new(service);
driver.Navigate().GoToUrl("");
driver.Manage().Window.Maximize();
Thread.Sleep(4000);

IReadOnlyCollection<IWebElement> visibleElements = driver.FindElements(By.CssSelector("*")).ToList();

ArrayList elementsTexts = new ArrayList();

foreach (var element in visibleElements)
{
    try
    {
        if (!element.Displayed)
        {
            continue;
        }
    }
    catch(Exception ex) {
        Console.WriteLine(ex);
        continue;
    }

    // Get the text and coordinates of the parent element
    string script = """
    var element = arguments[0];
    var text = '';
    var tagName = element.tagName.toLowerCase();
    let IsMerged = false;
    let elementPositions = [];

    function isElementVisible(element) {
        var style = window.getComputedStyle(element);
        return (style.display !== 'none' && style.visibility !== 'hidden');
    }

    if(!isElementVisible(element)) {
        return;
    }

    if (tagName === 'select') {
        if (element.selectedIndex !== -1) {
            text = element.options[element.selectedIndex].text;
        } else {
            text = element.getAttribute('placeholder') || '';
        }
        var rect = element.getBoundingClientRect();
        elementPositions.push({X: rect.x, Y: rect.y, Width: rect.width, Height: rect.height});
    } else if (tagName === 'input') {
        if (element.value.trim() === '') {
            text = element.getAttribute('placeholder') || '';
        } else {
            text = element.value;
        }
        var rect = element.getBoundingClientRect();
        elementPositions.push({X: rect.x, Y: rect.y, Width: rect.width, Height: rect.height});
    } else {
        for (var i = 0; i < element.childNodes.length; i++) {
            var childNode = element.childNodes[i];

            if (childNode.nodeType === Node.TEXT_NODE && childNode.nodeValue.length > 0) {
                text += childNode.nodeValue;
                var range = document.createRange();
                range.selectNodeContents(childNode);
                var rect = range.getBoundingClientRect();
                elementPositions.push({X: rect.x, Y: rect.y, Width: rect.width, Height: rect.height});
                range.detach(); 
            }
            else if(childNode.nodeType === Node.ELEMENT_NODE && childNode.tagName.toLowerCase() === 'a' && isElementVisible(childNode) && childNode.innerText.length > 0)  {
                text = text + " " + childNode.innerText;
                IsMerged = true;
                var rect = childNode.getBoundingClientRect();
                elementPositions.push({X: rect.x, Y: rect.y, Width: rect.width, Height: rect.height});
            }
            else if(childNode.nodeType === Node.ELEMENT_NODE && childNode.tagName.toLowerCase() === 'sup')  {
                text += childNode.innerText;
                IsMerged = true;
                var rect = childNode.getBoundingClientRect();
                elementPositions.push({X: rect.x, Y: rect.y, Width: rect.width, Height: rect.height});
            }
            else if(childNode.nodeType === Node.ELEMENT_NODE && childNode.tagName.toLowerCase() === 'b' && isElementVisible(childNode))  {
                text = text + " " + childNode.innerText;
                IsMerged = true;
                var rect = childNode.getBoundingClientRect();
                elementPositions.push({X: rect.x, Y: rect.y, Width: rect.width, Height: rect.height});
            }
            else if(childNode.nodeType === Node.ELEMENT_NODE && childNode.tagName.toLowerCase() === 'span' && isElementVisible(childNode))  {
                text = text + " " + childNode.innerText;
                IsMerged = true;
                var rect = childNode.getBoundingClientRect();
                elementPositions.push({X: rect.x, Y: rect.y, Width: rect.width, Height: rect.height});
            }
        }
    }

    var rect = element.getBoundingClientRect();
    return JSON.stringify({
        text: text.trim(),
        x: rect.x,
        y: rect.y,
        width: rect.width,
        height: rect.height,
        IsMerged: IsMerged,
        elementPositions: JSON.stringify(elementPositions)
    });
""";

    string resultString = (string)((IJavaScriptExecutor)driver).ExecuteScript(script, element);

    var resultDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);

    string parentText = resultDict["text"].ToString();
    double x = Convert.ToDouble(resultDict["x"]);
    double y = Convert.ToDouble(resultDict["y"]);
    double width = Convert.ToDouble(resultDict["width"]);
    double height = Convert.ToDouble(resultDict["height"]);
    bool IsMerged = (bool)resultDict["IsMerged"];
    List<Rectangle> elementPositions = JsonSerializer.Deserialize<List<Rectangle>>((string)resultDict["elementPositions"]);
    elementPositions = elementPositions.Where(e => e.X != 0 && e.Y != 0 && e.Width != 0 && e.Height != 0).ToList();


    double minX = 0;
    double minY = 0;
    double maxX = 0;
    double maxY = 0;

    try
    {
         minX = elementPositions.Min(r => r.X);
         minY = elementPositions.Min(r => r.Y);
         maxX = elementPositions.Max(r => r.X + r.Width);
         maxY = elementPositions.Max(r => r.Y + r.Height);
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex.Message);
        continue;
    }

    Rectangle combinedRectangle = new Rectangle(x = minX, y = minY, width = maxX - minX, height = maxY - minY);


    if (width == 0 || height == 0)
    {
        continue;
    }

    if (!string.IsNullOrWhiteSpace(parentText))
    {
        elementsTexts.Add(new Element(parentText, x, y, width, height, IsMerged, elementPositions, combinedRectangle));
    }
}

static bool Intersects(Rectangle rectA, Rectangle rectB)
{
    return !(rectA.X + rectA.Width < rectB.X || rectB.X + rectB.Width < rectA.X || rectA.Y + rectA.Height < rectB.Y || rectB.Y + rectB.Height < rectA.Y);
}

foreach (Element el in elementsTexts)
{
    string trimmedString = el.Text.Trim();
    string resultString = Regex.Replace(trimmedString, @"\s+", " ");
    el.Text = resultString;
}



var MergedItems = elementsTexts.Cast<Element>().ToList().Where(e => e.IsMerged);

foreach(Element el in MergedItems)
{
    var intersectingElements = elementsTexts.Cast<Element>().ToList().Where(e => Intersects(e.CombinedRectangle, el.CombinedRectangle) && e != el).ToList();
    el.IntersectingRectangles = intersectingElements;
    el.IntersectingRectangles = el.IntersectingRectangles.Where(e => !(el.Text.Equals(e.Text) && e.IsMerged)   ).ToList();
}

var DeleteableElements = new List<Element>();

foreach (Element el in MergedItems)
{
    foreach(Element intersect in el.IntersectingRectangles)
    {
        if(el.Text.Contains(intersect.Text))
        {
            DeleteableElements.Add(intersect);
        }
    }
}


List<Element> elementsTextsList = elementsTexts.Cast<Element>().ToList();

foreach (Element el in DeleteableElements)
{
    int index = elementsTextsList.IndexOf(el);

    if (index != -1)
    {
        elementsTextsList.RemoveAt(index);
    }
}

elementsTexts = new ArrayList(elementsTextsList);

var projectedElements = elementsTexts.Cast<Element>().ToList().Select(element => new
{
    element.Text,
    element.CombinedRectangle
}).ToList();

// Remove duplicates, exactly same rectangles and text.
List<FinalRectangle> final = new List<FinalRectangle>();

foreach (var el in projectedElements) {
    bool containsPerson = final.Any(
            person => 
            person.Text == el.Text && 
            person.X == el.CombinedRectangle.X &&
            person.Y == el.CombinedRectangle.Y &&
            person.Width == el.CombinedRectangle.Width &&
            person.Height == el.CombinedRectangle.Height
        );

    if(!containsPerson)
    {
        final.Add(new FinalRectangle(el.Text, el.CombinedRectangle.X, el.CombinedRectangle.Y, el.CombinedRectangle.Width, el.CombinedRectangle.Height));
    }
}


// remove duplicate rectangles, with some differences in x,y,w,h
foreach (var el in final)
{
    var intersects = final.Cast<FinalRectangle>().ToList().Where(e => Intersects(new Rectangle(e.X, e.Y, e.Width, e.Height), new Rectangle(el.X, el.Y, el.Width, el.Height))).ToList();
    el.Associated = intersects;
}


var final222 = new List<FinalRectangle>();

foreach (var el in final)
{
    int uniqueCount = el.Associated.Select(obj => obj.Text).Distinct().Count();

    if(uniqueCount == 1)
    {
        FinalRectangle smallestRectangle = el.Associated.OrderBy(rect => rect.Width * rect.Height).First();

        bool exists = final222.Any(r => r.X == smallestRectangle.X && r.Y == smallestRectangle.Y && r.Width == smallestRectangle.Width && r.Height == smallestRectangle.Height);

        if (!exists)
        {
            final222.Add(smallestRectangle);
        }
    }
    else
    {
        final222.Add(el);
    }
}

var printableElements = final222.Cast<FinalRectangle>().ToList().Select(element => new
{
    element.Text,
    element.X,
    element.Y,
    element.Width,
    element.Height
}).ToList();


var jsonArray = JsonSerializer.Serialize(printableElements, jso);
System.IO.File.WriteAllText("jsondata.txt", jsonArray, Encoding.UTF8);

driver.Quit();
Console.WriteLine("");