using System;
using System.Collections.Generic;

namespace PlayerTrack.Mock
{
    public class MockCategoryService : ICategoryService
    {
        public TrackCategory GetCategory(int categoryId)
        {
            throw new NotImplementedException();
        }

        public TrackCategory GetDefaultCategory()
        {
            throw new NotImplementedException();
        }

        public string[] GetCategoryNames()
        {
            throw new NotImplementedException();
        }

        public void MoveUpList(int id)
        {
            throw new NotImplementedException();
        }

        public void MoveDownList(int id)
        {
            throw new NotImplementedException();
        }

        public void AddCategory()
        {
            throw new NotImplementedException();
        }

        public void DeleteCategory(int index)
        {
            throw new NotImplementedException();
        }

        public void ResetCategories()
        {
            throw new NotImplementedException();
        }

        public List<TrackCategory> GetCategoriesCopy()
        {
            throw new NotImplementedException();
        }

        public TrackCategory GetCategoryByIndex(int categoryIndex)
        {
            throw new NotImplementedException();
        }
    }
}