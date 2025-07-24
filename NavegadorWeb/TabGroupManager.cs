using System.Collections.ObjectModel;
using System.Linq;

namespace NavegadorWeb
{
    /// <summary>
    /// Gestiona la colección de grupos de pestañas del navegador.
    /// </summary>
    public class TabGroupManager
    {
        public ObservableCollection<TabGroup> TabGroups { get; set; }

        public TabGroupManager()
        {
            TabGroups = new ObservableCollection<TabGroup>();
            // Crear un grupo por defecto al iniciar
            AddGroup("General");
        }

        /// <summary>
        /// Añade un nuevo grupo de pestañas.
        /// </summary>
        public TabGroup AddGroup(string name)
        {
            TabGroup newGroup = new TabGroup(name);
            TabGroups.Add(newGroup);
            return newGroup;
        }

        /// <summary>
        /// Elimina un grupo de pestañas.
        /// </summary>
        public void RemoveGroup(TabGroup group)
        {
            if (group != null && TabGroups.Contains(group))
            {
                // Mover las pestañas del grupo a otro grupo antes de eliminarlo,
                // o cerrarlas si es el único grupo.
                // Por simplicidad, aquí solo lo eliminamos. La lógica de reasignación
                // de pestañas debe manejarse en MainWindow.xaml.cs.
                TabGroups.Remove(group);
            }
        }

        /// <summary>
        /// Obtiene el grupo por defecto (el primero, o crea uno si no existe).
        /// </summary>
        public TabGroup GetDefaultGroup()
        {
            if (!TabGroups.Any())
            {
                return AddGroup("General");
            }
            return TabGroups.First();
        }
    }
}
