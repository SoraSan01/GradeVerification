using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Repository
{
    public interface IRepository<T> where T : class
    {
        Task<int> GetCountAsync();
    }
}
