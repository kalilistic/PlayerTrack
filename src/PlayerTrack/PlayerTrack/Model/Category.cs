using System.Numerics;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Colors;
using LiteDB;

namespace PlayerTrack
{
    /// <summary>
    /// Category for grouping players.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Category"/> class.
        /// </summary>
        /// <param name="id">unique id for category.</param>
        public Category(int id)
        {
            this.Id = id;
        }

        private Category()
        {
        }

        /// <summary>
        /// Gets or sets category id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets category name.
        /// </summary>
        public string Name { get; set; } = "New Category";

        /// <summary>
        /// Gets or sets category name as SeString.
        /// </summary>
        [BsonIgnore]
        public SeString? SeName { get; set; }

        /// <summary>
        /// Gets or sets category icon to display in list view.
        /// </summary>
        public int Icon { get; set; }

        /// <summary>
        /// Gets or sets category color for list view.
        /// </summary>
        public Vector4? ListColor { get; set; }

        /// <summary>
        /// Gets or sets category color name plates.
        /// </summary>
        public Vector4? NamePlateColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether alerts are enabled.
        /// </summary>
        public bool IsAlertEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether nameplates titles are enabled.
        /// </summary>
        public bool IsNamePlateTitleEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether nameplates colors are enabled.
        /// </summary>
        public bool IsNamePlateColorEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the default category for new players.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating category priority for force ranking.
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Reset category settings to default.
        /// </summary>
        public void Reset()
        {
            this.Icon = 0;
            this.ListColor = null;
            this.NamePlateColor = null;
            this.IsNamePlateColorEnabled = false;
            this.IsNamePlateTitleEnabled = false;
            this.IsAlertEnabled = false;
        }

        /// <summary>
        /// Create a copy of the category.
        /// </summary>
        /// <returns>Copy of category.</returns>
        public Category Copy()
        {
            var category = new Category
            {
                Id = this.Id,
                Name = this.Name,
                Icon = this.Icon,
                ListColor = this.ListColor,
                NamePlateColor = this.NamePlateColor,
                IsAlertEnabled = this.IsAlertEnabled,
                IsDefault = this.IsDefault,
            };
            category.SetSeName();
            return category;
        }

        /// <summary>
        /// Get effective category list color.
        /// </summary>
        /// <returns>list color.</returns>
        public Vector4 EffectiveListColor()
        {
            if (this.ListColor != null) return (Vector4)this.ListColor;
            return ImGuiColors.DalamudGrey;
        }

        /// <summary>
        /// Get effective category nameplate color.
        /// </summary>
        /// <returns>nameplate color.</returns>
        public Vector4 EffectiveNamePlateColor()
        {
            if (this.NamePlateColor != null) return (Vector4)this.NamePlateColor;
            return ImGuiColors.DalamudGrey;
        }

        /// <summary>
        /// Set SeString based on name.
        /// </summary>
        public void SetSeName()
        {
            this.SeName = new SeString(new Payload[]
            {
                new TextPayload($"《{this.Name}》"),
            });
        }
    }
}
