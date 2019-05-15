using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.ECS;
using Amazon.ECS.Model;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Yoda.WebSocket.Gateway
{
    public static class AwsUtility
    {
        public static async Task<string> GetContainerHostUrlAsync(IConfiguration configuration, string scheme = "http")
        {
            try
            {
                // This implementation is for local.
                var gatewayUrl = configuration["GATEWAY_URL"];

                if (!string.IsNullOrWhiteSpace(gatewayUrl))
                {
                    return gatewayUrl;
                }

                // Following implementation is for AWS-ECS.
                var uri = configuration["ECS_CONTAINER_METADATA_URI"];

                if (string.IsNullOrWhiteSpace(uri))
                {
                    throw new InvalidOperationException("ECS_CONTAINER_METADATA_URI does not exist.");
                }

                using (var client = new HttpClient())
                {
                    var response  = await client.GetAsync(uri);
                    var content   = await response.Content.ReadAsStringAsync();
                    var json      = JObject.Parse(content);
                    var cluster   = json["Labels"]["com.amazonaws.ecs.cluster"].ToString();
                    var taskArn   = json["Labels"]["com.amazonaws.ecs.task-arn"].ToString();
                    var ecs       = new AmazonECSClient();
                    var request   = new DescribeTasksRequest { Cluster = cluster, Tasks = new List<string> { taskArn } };
                    var tasks     = await ecs.DescribeTasksAsync(request);
                    var container = tasks.Tasks[0].Containers[0];
                    var address   = container.NetworkInterfaces[0].PrivateIpv4Address;

                    if (container.NetworkBindings.Any())
                    {
                        var port = container.NetworkBindings[0].HostPort;
                        return $"{scheme}://{address}:{port}/";
                    }
                    else
                    {
                        var taskDef = await ecs.DescribeTaskDefinitionAsync(
                            new DescribeTaskDefinitionRequest
                            {
                                TaskDefinition = tasks.Tasks[0].TaskDefinitionArn
                            });
                        var port = taskDef.TaskDefinition.ContainerDefinitions[0].PortMappings[0].HostPort;
                        return $"{scheme}://{address}:{port}/";
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }
    }
}
