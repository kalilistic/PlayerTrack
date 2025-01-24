using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;
using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class SocialListMemberRepository : BaseRepository
{
    public SocialListMemberRepository(IDbConnection connection, IMapper mapper) : base(connection, mapper) { }

    public List<SocialListMember> GetSocialListMembers(int socialListId)
    {
        const string sql = "SELECT * FROM social_list_members WHERE social_list_id = @social_list_id";
        var socialListMemberDTOs = Connection.Query<SocialListMemberDTO>(sql, new { social_list_id = socialListId }).ToList();
        return socialListMemberDTOs.Select(dto => Mapper.Map<SocialListMember>(dto)).ToList();
    }

    public List<SocialListMember> GetSocialListMembers(int socialListId, int pageNumber)
    {
        const string sql = "SELECT * FROM social_list_members WHERE social_list_id = @social_list_id AND page_number = @page_number";
        var socialListMemberDTOs = Connection.Query<SocialListMemberDTO>(sql, new { social_list_id = socialListId, page_number = pageNumber }).ToList();
        return socialListMemberDTOs.Select(dto => Mapper.Map<SocialListMember>(dto)).ToList();
    }

    public int CreateSocialListMember(SocialListMember member)
    {
        var socialListMemberDTO = Mapper.Map<SocialListMemberDTO>(member);
        SetCreateTimestamp(socialListMemberDTO);
        const string sql = @"
            INSERT INTO social_list_members (content_id, key, name, world_id, page_number, social_list_id, created, updated)
            VALUES (@content_id, @key, @name, @world_id, @page_number, @social_list_id, @created, @updated)
            RETURNING id;";
        return Connection.ExecuteScalar<int>(sql, socialListMemberDTO);
    }

    public void DeleteSocialListMember(int id)
    {
        const string sql = "DELETE FROM social_list_members WHERE id = @id";
        Connection.Execute(sql, new { id });
    }

    public void DeleteSocialListMembers(int socialListId)
    {
        const string sql = "DELETE FROM social_list_members WHERE social_list_id = @id";
        Connection.Execute(sql, new { id = socialListId });
    }
}
