using System.ServiceModel;

namespace WcfProject;

[ServiceContract]
public interface IUserService
{
    [OperationContract]  // Endpoint 1
    string GetUser(int id);

    [OperationContract]  // Endpoint 2
    void CreateUser(string name);

    [OperationContract]  // Endpoint 3
    void UpdateUser(int id, string name);

    [OperationContract]  // Endpoint 4
    void DeleteUser(int id);

    [OperationContract]  // Endpoint 5
    string[] ListUsers();

    [OperationContract]  // Endpoint 6
    bool ValidateUser(string username, string password);

    [OperationContract]  // Endpoint 7
    void ResetPassword(string username);

    [OperationContract]  // Endpoint 8
    string GetUserRole(int id);

    [OperationContract]  // Endpoint 9
    void AssignRole(int userId, string role);

    [OperationContract]  // Endpoint 10
    int GetUserCount();
}
