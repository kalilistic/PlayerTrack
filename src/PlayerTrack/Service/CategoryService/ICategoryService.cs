using System.Collections.Generic;

namespace PlayerTrack
{
    public interface ICategoryService
    {
        TrackCategory GetCategory(int categoryId);
        TrackCategory GetDefaultCategory();
        string[] GetCategoryNames();
        void MoveUpList(int id);
        void MoveDownList(int id);
        void AddCategory();
        void DeleteCategory(int index);
        void ResetCategories();
        List<TrackCategory> GetCategoriesCopy();
        TrackCategory GetCategoryByIndex(int categoryIndex);
    }
}