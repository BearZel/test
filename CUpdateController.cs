using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс обновляющий программное обеспечение контроллера
    /// </summary>
    class CUpdateController
    {
        //Сессия для работы системы обновления
        private CSession session = null;

        public CUpdateController(CSession session)
        {
            this.session = session;
        }

        
    }
}
