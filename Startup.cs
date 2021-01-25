using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;


namespace MicroService_Face_3_0
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        Arcsoft_Face_Action arcsoft_Face_Action;
        private string appID;
        private string faceKey;
        private int faceEngineNums;
        private int idEngineNums;
        private int aiEngineNums;
        private int requestQueueLimit;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            appID = Configuration.GetSection("AppSettings:AppId").Value;
            faceKey = Configuration.GetSection("AppSettings:FaceKey").Value;
            faceEngineNums = 0;
            int.TryParse(Configuration.GetSection("AppSettings:FaceEngineNum").Value, out faceEngineNums);
            idEngineNums = 0;
            int.TryParse(Configuration.GetSection("AppSettings:IDEngineNum").Value, out idEngineNums);
            aiEngineNums = 0;
            int.TryParse(Configuration.GetSection("AppSettings:AIEngineNum").Value, out aiEngineNums);
            arcsoft_Face_Action = new Arcsoft_Face_Action(appID, faceKey);
            requestQueueLimit = 100;
            int.TryParse(Configuration.GetSection("AppSettings:RequestQueueLimit").Value, out requestQueueLimit);
        }  

        // 怎么用
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            //用于传入的请求进行排队处理,避免线程池的不足.
            services.AddQueuePolicy(options =>
            {
                //最大并发请求数
                options.MaxConcurrentRequests = faceEngineNums;
                //请求队列长度限制
                options.RequestQueueLimit = requestQueueLimit;
            });

            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Arcsoft face 3.0 free version document",
                    Description = "3.0 APIs(free version)",
                    Version = "V 0.1"
                });
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlpath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                s.IncludeXmlComments(xmlpath);
            });

            Arcsoft_Face_Action enginePool = new Arcsoft_Face_Action(appID, faceKey);
            enginePool.Arcsoft_EnginePool(faceEngineNums, idEngineNums, aiEngineNums);
            services.AddArcSoftFaceService(enginePool);
        }

        // 需要用
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //添加并发限制中间件
            app.UseConcurrencyLimiter();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger(s => { });
            app.UseSwaggerUI(s =>
            {
                s.SwaggerEndpoint("/swagger/v1/swagger.json", "Arcsoft face 3.0 free version");
            });
        }
    }
}
