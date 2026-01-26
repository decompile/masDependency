using System.ServiceModel;

namespace MixedApiProject;

[ServiceContract]
public interface IOrderService
{
    [OperationContract]  // WCF Endpoint 1
    string GetOrder(int id);

    [OperationContract]  // WCF Endpoint 2
    void CreateOrder(string details);

    [OperationContract]  // WCF Endpoint 3
    void CancelOrder(int id);
}
