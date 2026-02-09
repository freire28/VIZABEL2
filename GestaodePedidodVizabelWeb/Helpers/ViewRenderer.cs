using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace GestaoPedidosVizabel.Helpers
{
    public static class ViewRenderer
    {
        public static async Task<string> RenderViewToStringAsync<TModel>(
            this Controller controller,
            string viewName,
            TModel model,
            bool isPartial = false)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                if (viewEngine == null)
                {
                    throw new InvalidOperationException("ICompositeViewEngine não encontrado");
                }

                var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !isPartial);

                if (!viewResult.Success)
                {
                    return $"A view com o nome {viewName} não foi encontrada";
                }

                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }
    }
}

