//Andrew Fryzel and Jared Nay

using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS9
{
    class BoggleClientApplicationContext : ApplicationContext
    {

        private int windowCount = 0;
        private static BoggleClientApplicationContext context;

        private BoggleClientApplicationContext()
        {
        }

        public static BoggleClientApplicationContext GetContext()
        {
            if (context == null)
            {
                context = new BoggleClientApplicationContext();
            }
            return context;
        }

        public void RunNew()
        {
            // Create the window and the controller
            BoggleClient window = new BoggleClient();

            new Controller(window);

            // One more form is running
            windowCount++;

            // When this form closes, we want to find out
            window.FormClosed += (o, e) => { if (--windowCount <= 0) ExitThread(); };

            // Run the form
            window.Show();

        }
    }
}
