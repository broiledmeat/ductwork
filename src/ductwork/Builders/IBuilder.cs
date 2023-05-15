using System;
using System.Collections.Generic;

namespace ductwork.Builders;

public interface IBuilder
{
    IEnumerable<Exception> Validate();
    Graph GetGraph();
}