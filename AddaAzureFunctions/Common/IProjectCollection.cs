using System;
using System.Threading.Tasks;

namespace ADDA.Common
{
    public interface IProjectCollection<T>
    {
        Uri Uri { get; set; }

        Task<T> GetCredential();
        void GetUri();
    }
}