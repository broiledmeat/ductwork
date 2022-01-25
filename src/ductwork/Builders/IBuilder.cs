using System;
using System.Collections.Generic;

#nullable enable
namespace ductwork.Builders;

public interface IBuilder
{
    IEnumerable<Exception> Validate();
    Graph GetGraph();
}