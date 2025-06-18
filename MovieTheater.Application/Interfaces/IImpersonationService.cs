using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Application.Interfaces
{
    public interface IImpersonationService
    {
        Task<bool> StartImpersonationAsync(Guid targetUserId, string reason);
        Task<bool> StopImpersonationAsync();
        Guid GetEffectiveUserId();
        bool IsImpersonating();
        Guid? GetImpersonatedBy();
    }
}
