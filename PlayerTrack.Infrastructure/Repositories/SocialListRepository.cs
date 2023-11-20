using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;
using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class SocialListRepository : BaseRepository
{
    public SocialListRepository(IDbConnection connection, IMapper mapper)
            : base(connection, mapper)
        {
        }

        public SocialList? GetSocialList(int id)
        {
            const string sql = "SELECT * FROM social_lists WHERE id = @id";
            var socialListDTO = this.Connection.QuerySingleOrDefault<SocialListDTO>(sql, new { id });
            return socialListDTO == null ? null : this.Mapper.Map<SocialList>(socialListDTO);
        }
        
        public SocialList? GetSocialList(ulong contentId, SocialListType socialListType)
        {
            const string sql = "SELECT * FROM social_lists WHERE content_id = @content_id AND list_type = @list_type";
            var socialListDTO = this.Connection.QuerySingleOrDefault<SocialListDTO>(sql, new { content_id = contentId, list_type = socialListType });
            return socialListDTO == null ? null : this.Mapper.Map<SocialList>(socialListDTO);
        }
        
        public List<SocialList> GetSocialLists(ulong contentId)
        {
            const string sql = "SELECT * FROM social_lists WHERE content_id = @content_id";
            var socialListDTOs = this.Connection.Query<SocialListDTO>(sql, new { content_id = contentId }).ToList();
            return socialListDTOs.Select(dto => this.Mapper.Map<SocialList>(dto)).ToList();
        }

        public List<SocialList> GetSocialListsWithDefaultCategory(int categoryId)
        {
            const string sql = "SELECT * FROM social_lists WHERE default_category_id = @default_category_id";
            var socialListDTOs = this.Connection.Query<SocialListDTO>(sql, new { default_category_id = categoryId }).ToList();
            return socialListDTOs.Select(dto => this.Mapper.Map<SocialList>(dto)).ToList();
        }

        public SocialList? GetSocialList(ulong contentId, SocialListType socialListType, ushort listNumber, uint dataCenterId)
        {
            const string sql = "SELECT * FROM social_lists WHERE content_id = @content_id AND list_type = @list_type AND data_center_id = @data_center_id AND list_number = @list_number";
            var socialListDTO = this.Connection.QuerySingleOrDefault<SocialListDTO>(sql, new { content_id = contentId, list_type = socialListType, data_center_id = dataCenterId, list_number = listNumber });
            return socialListDTO == null ? null : this.Mapper.Map<SocialList>(socialListDTO);
        }

        public SocialList? GetSocialList(ulong contentId, SocialListType socialListType, ushort listNumber)
        {
            const string sql = "SELECT * FROM social_lists WHERE content_id = @content_id AND list_type = @list_type AND list_number = @list_number";
            var socialListDTO = this.Connection.QuerySingleOrDefault<SocialListDTO>(sql, new { content_id = contentId, list_type = socialListType, list_number = listNumber });
            return socialListDTO == null ? null : this.Mapper.Map<SocialList>(socialListDTO);
        }
        
        public int CreateSocialList(SocialList socialList)
        {
            var socialListDTO = this.Mapper.Map<SocialListDTO>(socialList);
            SetCreateTimestamp(socialListDTO);
            const string sql = @"
                INSERT INTO social_lists (content_id, list_type, data_center_id, list_number, page_count, add_players, sync_with_category, default_category_id, page_last_updated, created, updated)
                VALUES (@content_id, @list_type, @data_center_id, @list_number, @page_count, @add_players, @sync_with_category, @default_category_id, @page_last_updated, @created, @updated) RETURNING id";
            return this.Connection.ExecuteScalar<int>(sql, socialListDTO);
        }

        public void UpdateSocialList(SocialList socialList)
        {
            var socialListDTO = this.Mapper.Map<SocialListDTO>(socialList);
            SetUpdateTimestamp(socialListDTO);
            const string sql = @"
                UPDATE social_lists
                SET list_type = @list_type,
                    data_center_id = @data_center_id,
                    page_count = @page_count,
                    add_players = @add_players,
                    sync_with_category = @sync_with_category,
                    default_category_id = @default_category_id,
                    page_last_updated = @page_last_updated,
                    updated = @updated
                WHERE id = @id;";
            this.Connection.Execute(sql, socialListDTO);
        }

        public void DeleteSocialList(int id)
        {
            const string sql = "DELETE FROM social_lists WHERE id = @id";
            this.Connection.Execute(sql, new { id });
        }
    
}