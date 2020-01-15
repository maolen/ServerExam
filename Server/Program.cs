using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace Server
{
    class Program
    {
        public static string IncomeUri { get; set; }
        static async Task Main(string[] args)
        {
            var users = new List<User>();
            using var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:80/");
            listener.Start();
            const string GIVE = "GIVE";
            const string ADD = "ADD";
            const string UPDATE = "UPDATE";
            const string REMOVE = "REMOVE";
            using var contextDb = new ContextDb();
            Console.WriteLine("Готов к работе");

            while (true)
            {
                users.Clear();
                users = contextDb.Users.Where(u => u.IsDeleted == false).ToList();
                using var context = await listener.GetContextAsync();

                var request = context.Request;
                IncomeUri = request.RawUrl;
                var response = context.Response;

                //using var stream = client.GetStream();
                //var resultText = string.Empty;
                //var buffer = new byte[1024];
                //stream.Read(buffer, 0, buffer.Length);
                ////resultText += Encoding.UTF8.GetString(buffer).TrimEnd('\0');

                //var response = JsonConvert.DeserializeObject<Response>(resultText);

                if (response.Path == "user")
                {
                    if (response.Action == GIVE)
                    {
                        StringBuilder stringBuilder = ParseSpecialType(users);
                        var answer = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                        stream.Write(answer, 0, answer.Length);
                        Console.WriteLine($"[{DateTime.Now}]\tОтправка таблицы с пользователями...");
                    }
                    else if (response.Action == ADD)
                    {
                        var user = new User()
                        {
                            Name = response.Value,
                        };
                        contextDb.Users.Add(user);
                        contextDb.SaveChanges();
                        Console.WriteLine($"[{DateTime.Now}]\tДобавления пользователя {user.Id} - {user.Name}...");
                    }
                    else if (response.Action == UPDATE)
                    {
                        try
                        {
                            var user = users.FirstOrDefault(x => x.Id == int.Parse(response.Value));
                            var oldName = user.Name;
                            user.Name = response.NewData;
                            contextDb.Update(user);
                            contextDb.SaveChanges();
                            Console.WriteLine($"[{DateTime.Now}]\tОбновление пользователя {user.Id} - {oldName} на {user.Id} - {response.NewData}...");
                        }
                        catch
                        {
                            var error = "Вы ввели неверный ID";
                            var answer = Encoding.UTF8.GetBytes(error);
                            stream.Write(answer, 0, answer.Length);
                        }
                    }
                    else if (response.Action == REMOVE)
                    {
                        try
                        {
                            var user = contextDb.Users.FirstOrDefault(x => x.Id == int.Parse(response.Value));
                            user.IsDeleted = true;
                            contextDb.Update(user);
                            contextDb.SaveChanges();
                            Console.WriteLine($"[{DateTime.Now}]\tУдаление пользователя {user.Id} - {user.Name}...");
                        }
                        catch
                        {
                            var error = "Вы ввели неверный ID";
                            var answer = Encoding.UTF8.GetBytes(error);
                            stream.Write(answer, 0, answer.Length);
                        }
                    }
                }

                try
                {
                    switch (context.Request.Url.AbsolutePath)
                    {
                        case "/users/give/":
                            if (context.Request.HttpMethod == POST)
                            {

                            }

                            break;
                        case "/users/add/":
                            if (context.Request.HttpMethod == ADD)
                            {
                                try
                                {
                                    using var body = context.Request.InputStream;
                                    using var reader = new StreamReader(body, context.Request.ContentEncoding);

                                    var json = reader.ReadToEnd();
                                    var user = JsonConvert.DeserializeObject<User>(json);

                                    var userDb = contextDb.Users.FirstOrDefault(x => x.Name == user.Name);
                                    if (userDb == null)
                                    {
                                        await contextDb.Users.AddAsync(user);
                                        await contextDb.SaveChangesAsync();
                                        Console.WriteLine($"[{DateTime.Now}]\tДобавления пользователя {user.Id} - {user.Name}...");
                                        response.StatusCode = (int)HttpStatusCode.Created;
                                    }
                                    else
                                    {
                                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                                    }
                                }
                                catch (Exception exception)
                                {
                                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                                    //var exceptionJson = JsonConvert.SerializeObject<Exception>(exception);

                                }

                            }
                            break;
                        case "/users/update":
                            if (context.Request.HttpMethod == POST)
                            {
                                try
                                {
                                    using var body = context.Request.InputStream;
                                    using var reader = new StreamReader(body, context.Request.ContentEncoding);

                                    var json = reader.ReadToEnd();
                                    var user = JsonConvert.DeserializeObject<User>(json);
                                    var user = contextDb.Users.FirstOrDefault(x => x.Id == int.Parse(user.Id));
                                    contextDb.Update(user);
                                    await contextDb.SaveChangesAsync();
                                    response.StatusCode = (int)HttpStatusCode.OK;
                                }
                                catch
                                {
                                    response.StatusCode = (int)HttpStatusCode.NotFound;
                                }
                            }
                            break;
                        case "/users/delete":
                            if (context.Request.HttpMethod == POST)
                            {

                            }
                            break;
                        default:
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                    }

                }
                catch (Exception exception)
                {
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.ContentType = "application/json";
                    var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(exception));
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }

                response.Close();
            }
        }
    }
}

public static StringBuilder ParseSpecialType(List<User> users)
{
    StringBuilder stringBuilder = new StringBuilder();
    stringBuilder.Append("[table]\n");
    stringBuilder.Append("\t[header]\n");
    stringBuilder.Append("\t\t[h]ID[/h]\n");
    stringBuilder.Append("\t\t[h]Name[/h]\n");
    stringBuilder.Append("\t[/header]\n");
    stringBuilder.Append("\t[data]\n");
    foreach (var item in users)
    {
        stringBuilder.Append($"\t\t[d]{item.Id}[/d]\n");
        stringBuilder.Append($"\t\t[d]{item.Name}[/d]\n");
    }
    stringBuilder.Append("\t[/data]\n");
    stringBuilder.Append("[/table]");
    return stringBuilder;
}
}
}
