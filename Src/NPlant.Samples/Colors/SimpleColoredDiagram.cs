using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPlant.Samples.Colors
{
    public class SimpleColoredDiagram : ClassDiagram
    {
        public SimpleColoredDiagram()
        {
            this.AddClass<Foo>().Color = "red";
            this.AddClass<Bar>().Color = "blue";
            this.AddClass<Baz>().Color = "blue";
        }
    }

    public class Foo
    {
    }

    public class Bar : Foo
    {
    }

    public class Baz : Foo
    {
    }
}
