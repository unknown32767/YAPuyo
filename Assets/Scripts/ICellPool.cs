using System.Collections.Generic;

public interface ICellPool<T> where T : class, ICell
{
    T Take();

    List<T> Take(int count);
}