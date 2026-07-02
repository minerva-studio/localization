using System;
using System.Collections.Generic;

namespace Minerva.Localizations.Editor.Utilities
{
    public class GenericListPageList<T> : PageList
    {
        private readonly Action<T> drawer;

        public List<T> entryList;

        public override int Size => entryList.Count;

        public GenericListPageList(List<T> entryList, Action<T> drawer, int linesPerPage = 10)
        {
            windowMinWidth = 100;
            this.entryList = entryList;
            this.drawer = drawer;
            LinesPerPage = linesPerPage;
        }

        protected override void DrawElement(int index)
        {
            drawer.Invoke(entryList[index]);
        }

        public override void AddElement()
        {
            entryList.Add(entryList[Size - 1]);
        }
    }
}
