﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSEDiagnosticLibrary
{
    public interface IProperties
    {
        IDictionary<string, object> Properties { get; }
    }
}
