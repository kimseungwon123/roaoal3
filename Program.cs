using System;
using MySql.Data.MySqlClient;

namespace mysql1
{
    class Program
    {
        static string connectionString = "Server=localhost;Port=3306;Database=mydb;Uid=root;Pwd=root";
        static void Main(string[] args)
        {
            Console.Write("1. 검색, 2. 삽입, 3. 삭제, 4. 전체조회");
            int ansMenu = Convert.ToInt32(Console.ReadLine());
            

            // SQL: game_info와 game_price를 조인해서 이름과 가격을 가져옵니다.
            // LIKE 연산자를 사용해 부분 일치 검색을 합니다.


            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                switch (ansMenu)
                {
                    case 1:
                        Console.WriteLine("\n--- 검색 메뉴 ---");
                        Console.WriteLine("1. 제목  2. 가격(이하)  3. 내점수(일치)  4. 대중평가(이상)  5. 플레이시간(이상)  6. 장르(포함)");
                        Console.Write("검색 조건을 선택하세요: ");

                        if (!int.TryParse(Console.ReadLine(), out int searchType) || searchType < 1 || searchType > 6)
                        {
                            Console.WriteLine("잘못된 입력입니다.");
                            break;
                        }

                        string baseQuery = @"
                            SELECT G.id, G.name, {0} AS info  
                            FROM game_info G
                            LEFT JOIN game_price P ON G.id = P.game_info_id
                            LEFT JOIN private_estimate Prv ON G.id = Prv.game_info_id
                            LEFT JOIN public_estimate Pub ON G.id = Pub.game_info_id
                            LEFT JOIN genre Ge ON G.id = Ge.game_info_id
                            GROUP BY G.id
                            HAVING {1}";

                        string selectColumn = ""; // {0}에 들어갈 내용
                        string havingClause = "";  // {1}에 들어갈 내용
                        string inputMsg = "";     // 사용자에게 보여줄 메시지
                        object paramValue = null; // @value에 넣을 실제 값
                        string unit = "";         // 결과값의 단위

                        switch (searchType)
                        {
                            case 1: // 제목
                                selectColumn = "IFNULL(P.price, 0)";
                                havingClause = "G.name LIKE @value"; 
                                inputMsg = "검색할 제목을 입력하세요.: ";
                                unit = "원";
                                break;
                            case 2: // 가격
                                selectColumn = "IFNULL(P.price, 0)";
                                havingClause = "info <= @value";
                                inputMsg = "얼마 이하의 게임을 찾으시나요? (예: 10000): ";
                                unit = "원";
                                break;
                            case 3: // 내 평점
                                selectColumn = "IFNULL(Prv.rate, 0)";
                                havingClause = "info = @value";
                                inputMsg = "몇 점(0~5) 준 게임을 찾으시나요?: ";
                                unit = "점";
                                break;
                            case 4: // 대중 평가
                                selectColumn = "IFNULL(Pub.positive_rate, 0)";
                                havingClause = "info >= @value";
                                inputMsg = "긍정 평가 몇 % 이상을 찾으시나요?: ";
                                unit = "%";
                                break;
                            case 5: // 플레이 타임
                                selectColumn = "IFNULL(Prv.playtime, 0)";
                                havingClause = "info >= @value";
                                inputMsg = "몇 시간 이상 플레이한 게임을 찾으시나요?: ";
                                unit = "시간";
                                break;
                            case 6: // 장르, GROUP_CONCAT 적용
                                selectColumn = "GROUP_CONCAT(DISTINCT Ge.genre SEPARATOR ', ')";
                                havingClause = "info LIKE @value";
                                inputMsg = "찾고 싶은 장르를 입력하세요.: ";
                                unit = "";
                                break;
                        }

                        Console.Write(inputMsg);
                        string userInput = Console.ReadLine();
                        // 조건에 따른 value, 1 == 제목, 6 == 장르
                        if (searchType == 1 || searchType == 6)
                        {
                            paramValue = "%" + userInput + "%";
                        }
                        else // 도메인 체크
                        {
                            decimal.TryParse(userInput, out decimal tempNum);
                            paramValue = tempNum;
                        }

                        string finalQuery = string.Format(baseQuery, selectColumn, havingClause);

                        try
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(finalQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@value", paramValue);

                                using (MySqlDataReader reader = cmd.ExecuteReader())
                                {
                                    Console.WriteLine("\n--- 검색 결과 ---");
                                    if (searchType == 6) 
                                        Console.WriteLine("{0, -4} | {1, -40} | {2}", "ID", "게임명", "장르 목록");
                                    else
                                        Console.WriteLine("{0, -4} | {1, -30} | {2}", "ID", "게임명", "검색정보");

                                    Console.WriteLine("------------------------------------------------");

                                    while (reader.Read())
                                    {
                                        string id = reader[0].ToString();
                                        string name = reader[1].ToString();
                                        if (name.Length > 28) name = name.Substring(0, 26) + "..";

                                        string info = reader[2].ToString();

                                        // 장르일 때 장르 자름
                                        if (searchType == 6 && info.Length > 40)
                                            info = info.Substring(0, 37) + "..";

                                        Console.WriteLine("{0, -4} | {1, -30} | {2}{3}", id, name, info, unit);
                                    }
                                    Console.WriteLine("------------------------------------------------");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("에러 발생: " + ex.Message);
                        }
                        break;

                    case 2:
                        Console.WriteLine("\n--- 게임 추가 ---");

                        Console.Write("게임 이름: ");
                        string newName = Console.ReadLine();

                        Console.Write("출시일 YYMMDD (예: 241121): ");
                        if (!int.TryParse(Console.ReadLine(), out int newYear))
                        {
                            Console.WriteLine("출시일은 숫자로 입력하십시오.");
                            break;
                        }

                        Console.Write("가격 (원): ");
                        if (!int.TryParse(Console.ReadLine(), out int newPrice))
                        {
                            Console.WriteLine("가격은 숫자로 입력하십시오.");
                            break;
                        }

                        conn.Open();
                        MySqlTransaction transaction = conn.BeginTransaction();

                        try
                        {
                            // LAST_INSERT_ID() 함수를 써서 방금 들어간 AUTO_INCREMENT인 ID값을 가져온다.
                            string insertInfoQuery = @"
                            INSERT INTO game_info (name, create_time) VALUES (@name, @year);
                            SELECT LAST_INSERT_ID();";

                            int newGameId = 0;

                            using (MySqlCommand cmdInfo = new MySqlCommand(insertInfoQuery, conn, transaction))
                            {
                                cmdInfo.Parameters.AddWithValue("@name", newName);
                                cmdInfo.Parameters.AddWithValue("@year", newYear);

                                // ExecuteScalar를 통해 현재 id값 반환 -- SELECT LAST_INSERT_ID();
                                newGameId = Convert.ToInt32(cmdInfo.ExecuteScalar());
                            }

                            // game_price 테이블에 가격 등록
                            string insertPriceQuery = @"
                            INSERT INTO game_price (game_info_id, price, discounted_price) 
                            VALUES (@id, @price, @price)"; // 일단 할인가 = 정가로 넣음

                            using (MySqlCommand cmdPrice = new MySqlCommand(insertPriceQuery, conn, transaction))
                            {
                                cmdPrice.Parameters.AddWithValue("@id", newGameId);
                                cmdPrice.Parameters.AddWithValue("@price", newPrice);
                                cmdPrice.ExecuteNonQuery(); //변환된 레코드의 개수, 쿼리 실행용, 반환값 사용하지 않음
                            }

                            transaction.Commit();
                            Console.WriteLine($"\n성공! [{newName}] 게임이 ID {newGameId}번으로 등록되었습니다.");
                        }
                        catch (Exception ex)
                        {
                            // 에러가 나면 등록 취소 (롤백)
                            transaction.Rollback(); //설정하지 않아도 자동으로 됨, 명시적으로 작성한 것
                            Console.WriteLine("등록 실패 (취소됨): " + ex.Message);
                        }
                        break;
                    case 3:
                        string querydelect = @"DELETE FROM game_info
                                               WHERE id = @ID";
                        Console.Write("제거할 게임 id를 입력하십시오 : ");
                        string removeidStr = Console.ReadLine();
                        if (int.TryParse(removeidStr, out int removeid))
                        { 
                            try
                            {
                                conn.Open();

                                using (MySqlCommand cmd = new MySqlCommand(querydelect, conn))
                                {
                                    cmd.Parameters.AddWithValue("@ID", removeid);
                                    if (cmd.ExecuteNonQuery() == 1)
                                    {
                                        Console.WriteLine("제거 성공");
                                    }
                                    else
                                    {
                                        Console.WriteLine("삭제 실패: 해당 ID를 가진 게임이 없습니다.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("에러 발생: " + ex.Message);
                            }
                        }
                        else
                        {
                            Console.WriteLine("숫자를 입력하십시오.");
                        }
                        break;
                    case 4:
                        Console.WriteLine("\n--- 전체 게임 목록 (통합 조회) ---");
                        

                        //game_info에 left join
                        //GROUP_CONCAT, GROUP BY G.id를 통해서 장르 정리
                        string queryAll = @"
                        SELECT 
                            G.id, 
                            G.name, 
                            IFNULL(P.price, 0) AS price,
                            IFNULL(C.name, '알수없음') AS company,
                            IFNULL(Prv.rate, 0) AS my_rate,
                            IFNULL(Pub.positive_rate, 0) AS public_score,
                            GROUP_CONCAT(Ge.genre SEPARATOR ', ') AS genres
                        FROM game_info G
                        LEFT JOIN game_price P ON G.id = P.game_info_id
                        LEFT JOIN game_company GC ON G.id = GC.game_info_id
                        LEFT JOIN game_company_info C ON GC.company_name = C.name
                        LEFT JOIN private_estimate Prv ON G.id = Prv.game_info_id
                        LEFT JOIN public_estimate Pub ON G.id = Pub.game_info_id
                        LEFT JOIN genre Ge ON G.id = Ge.game_info_id
                        GROUP BY G.id
                        ORDER BY G.id ASC";

                        try
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(queryAll, conn))
                            {
                                using (MySqlDataReader reader = cmd.ExecuteReader())
                                {
                                    // 포멧 고정 - 칸 간격 맞춤.
                                    Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");
                                    Console.WriteLine("{0, -4} | {1, -22} | {2, -10} | {3, -12} | {4, -5} | {5, -5} | {6, -40}",
                                        "ID", "게임명", "가격", "제작사", "내점수", "긍정%", "장르 (40자 제한)");
                                    Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");

                                    while (reader.Read())
                                    {
                                        string id = reader["id"].ToString();

                                        // 게임명
                                        string name = reader["name"].ToString();
                                        if (name.Length > 20) name = name.Substring(0, 18) + "..";

                                        // 가격
                                        string price = Convert.ToInt32(reader["price"]).ToString("N0") + "원";

                                        // 제작사
                                        string company = reader["company"].ToString();
                                        if (company.Length > 10) company = company.Substring(0, 8) + "..";

                                        // 평점
                                        string myRate = reader["my_rate"].ToString();
                                        string pubScore = reader["public_score"].ToString();

                                        // 장르
                                        string genres = reader["genres"].ToString();
                                        if (genres.Length > 38) genres = genres.Substring(0, 36) + "..";

                                        // 포멧 고정
                                        Console.WriteLine("{0, -4} | {1, -22} | {2, -10} | {3, -12} | {4, -5} | {5, -5} | {6, -40}",
                                            id, name, price, company, myRate, pubScore, genres);
                                    }
                                    Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("조회 실패: " + ex.Message);
                        }
                        break;
                }             
                Console.WriteLine("\n종료하려면 엔터를 누르세요...");
                Console.ReadLine();
            }

        }
    }
}
