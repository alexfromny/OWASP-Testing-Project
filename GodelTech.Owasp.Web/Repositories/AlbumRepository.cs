﻿using GodelTech.Owasp.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace GodelTech.Owasp.Web.Repositories
{
    public class AlbumRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["Owasp.MusicStore"].ConnectionString;

        public Album Get(string id)
        {
            // id is straight from the URI
            var sql = "SELECT * FROM Album WHERE AlbumId = " + id;

            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();

                    var reader = cmd.ExecuteReader();
                    var record = GetRecords(reader).SingleOrDefault();
                    return record;
                }
            }
        }

        public IEnumerable<Album> GetList(string searchKey)
        {
            var sql = @"SELECT * FROM Album 
                        INNER JOIN Artist on Artist.ArtistId = Album.ArtistId
                        WHERE Title like '%" + searchKey + "%'";

            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();

                    var reader = cmd.ExecuteReader();
                    var records = GetRecords(reader);
                    return records;
                }
            }
        }

        public IEnumerable<Album> GetList(int genreId)
        {
            var sql = @"SELECT * FROM Album
                        INNER JOIN Artist on Artist.ArtistId = Album.ArtistId
                        WHERE GenreId = @GenreId";

            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@GenreId", genreId);

                    connection.Open();

                    var reader = cmd.ExecuteReader();
                    var records = GetRecords(reader);
                    return records;
                }
            }
        }

        public IEnumerable<Album> GetList(int skip, int take)
        {
            var sql = @"SELECT * FROM Album
                        INNER JOIN Artist on Artist.ArtistId = Album.ArtistId
                        ORDER BY CURRENT_TIMESTAMP
                        OFFSET @Skip ROWS
                        FETCH NEXT @Take ROWS ONLY";

            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Skip", skip);
                    cmd.Parameters.AddWithValue("@Take", take);

                    connection.Open();

                    var reader = cmd.ExecuteReader();
                    var records = GetRecords(reader);
                    return records;
                }
            }
        }

        public int AddIfNotExist(IEnumerable<Album> albums)
        {
            var enumerable = albums.ToList();

            if (albums == null || !enumerable.Any())
            {
                throw new ArgumentNullException(nameof(albums));
            }

            var albumsValues = string.Join(",", enumerable.Select(x => $"({x.ArtistId}, {x.GenreId}, '{x.Title}', {x.Price}, '{x.AlbumArtUrl}')"));

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("MERGE INTO Album AS Target");
            stringBuilder.AppendLine($"USING (VALUES {albumsValues})");
            stringBuilder.AppendLine("AS Source (GenreId, ArtistId, Title, Price, AlbumArtUrl)");
            stringBuilder.AppendLine("ON Target.GenreId = Source.GenreId");
            stringBuilder.AppendLine("AND Target.ArtistId = Source.ArtistId");
            stringBuilder.AppendLine("AND Target.Title = Source.Title");
            stringBuilder.AppendLine("AND Target.Price = Source.Price");
            stringBuilder.AppendLine("AND Target.AlbumArtUrl = Source.AlbumArtUrl");
            stringBuilder.AppendLine("WHEN NOT MATCHED BY TARGET THEN");
            stringBuilder.AppendLine("INSERT (GenreId, ArtistId, Title, Price, AlbumArtUrl)");
            stringBuilder.AppendLine("VALUES (source.GenreId, source.ArtistId, source.Title, source.Price, source.AlbumArtUrl);");

            var sql = stringBuilder.ToString();

            using (var connection = new SqlConnection(connectionString))
            {
                using (var cmd = new SqlCommand(sql, connection))
                {
                    connection.Open();

                    return cmd.ExecuteNonQuery();
                }
            }
        }

        private static IEnumerable<Album> GetRecords(IDataReader reader)
        {
            var records = new List<Album>();

            while (reader.Read())
            {
                var record = BuildAlbum(reader);
                records.Add(record);
            }

            return records;
        }

        private static Album BuildAlbum(IDataReader reader)
        {
            return new Album
            {
                AlbumId = GetFieldValue<int>(reader, nameof(Album.AlbumId)),
                GenreId = GetFieldValue<int>(reader, nameof(Album.GenreId)),
                ArtistId = GetFieldValue<int>(reader, nameof(Album.ArtistId)),
                Title = GetFieldValue<string>(reader, nameof(Album.Title)),
                Price = GetFieldValue<decimal>(reader, nameof(Album.Price)),
                Artist = BuildArtist(reader)
            };
        }

        private static Artist BuildArtist(IDataReader reader)
        {
            return new Artist
            {
                ArtistId = GetFieldValue<int>(reader, nameof(Artist.ArtistId)),
                Name = GetFieldValue<string>(reader, nameof(Artist.Name))
            };
        }

        private static T GetFieldValue<T>(IDataReader reader, string columnName, T defaultValue = default(T))
        {
            var obj = reader[columnName];

            if (obj == null || obj == DBNull.Value)
                return defaultValue;

            return (T)obj;
        }
    }
}