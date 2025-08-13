using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;

namespace MVVMLightLearning.ViewModels
{
    /// <summary>  
    /// This class contains static references to all the view models in the  
    /// application and provides an entry point for the bindings.  
    /// </summary>  
    public class ViewModelLocator
    {
        /// <summary>  
        /// 静态构造函数 - 应用启动时执行一次  
        /// </summary>  
        static ViewModelLocator()
        { 
            if (!SimpleIoc.Default.IsRegistered<MainViewModel>())
            {
                SimpleIoc.Default.Register<MainViewModel>();
            }
        }

        /// <summary>  
        /// 实例构造函数  
        /// </summary>  
        public ViewModelLocator()
        {
            // 注册 ViewModel  
            RegisterViewModels();
        }

        private void RegisterViewModels()
        {
            // 注册 ViewModels
            if (!SimpleIoc.Default.IsRegistered<MainViewModel>())
            {
                SimpleIoc.Default.Register<MainViewModel>();
            }
        }

        /// <summary>  
        /// 获取 MainViewModel 的实例  
        /// </summary>  
        public MainViewModel Main => SimpleIoc.Default.GetInstance<MainViewModel>();

        /// <summary>  
        /// 清理所有资源  
        /// </summary>  
        public static void Cleanup()
        {
            // TODO Clear the ViewModels  
            SimpleIoc.Default.Reset();
        }
    }
}