﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pathfinder.Abstraction
{
   public interface IMapGenerator
    {
        IMap DefineMap(string argument = "",DiagonalMovement? diagonal = null);
    }
}
