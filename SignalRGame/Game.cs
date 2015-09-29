using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TicTacToe
{
    public class Client
    {
        public string Name { get; set; }
        public Client Opponent { get; set; }
        public bool IsPlaying { get; set; }
        public bool WaitingForMove { get; set; }
        public bool LookingForOpponent { get; set; }

        public string ConnectionId { get; set; }
    }

    public class GameInformation
    {
        public string OpponentName { get; set; }

        public string Winner { get; set; }

        public int MarkerPosition { get; set; }
    }

    public class Game : Hub
    {
        public static List<Client> _clients = new List<Client>();
        public static List<TicTacToe> games = new List<TicTacToe>();

        private object _syncRoot = new object();
        private static int _gamesPlayed = 0;

        public Task OnDisconnected()
        {
            var game = games.FirstOrDefault(x => x.Player1.ConnectionId == Context.ConnectionId || x.Player2.ConnectionId == Context.ConnectionId);
            if (game == null)
            {
                // Client without game?
                var clientWithoutGame = _clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (clientWithoutGame != null)
                {
                    _clients.Remove(clientWithoutGame);

                    SendStatsUpdate();
                }
                return null;
            }

            if (game != null)
            {
                games.Remove(game);
            }

            var client = game.Player1.ConnectionId == Context.ConnectionId ? game.Player1 : game.Player2;

            if (client == null) return null;

            _clients.Remove(client);
            if (client.Opponent != null)
            {
                SendStatsUpdate();
                return Clients.Client(client.Opponent.ConnectionId).opponentDisconnected(client.Name);
            }
            return null;
        }

        public override Task OnConnected()
        {
            return SendStatsUpdate();
        }

        public Task SendStatsUpdate()
        {
            return Clients.All.refreshAmountOfPlayers(new { totalGamesPlayed = _gamesPlayed, amountOfGames = games.Count, amountOfClients = _clients.Count });
        }

        public void RegisterClient(string data)
        {
            lock(_syncRoot)
            {
                var client = _clients
                    .FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                if (client == null)
                {
                    client = new Client { ConnectionId = Context.ConnectionId, Name = data };
                    _clients.Add(client);
                }

                client.IsPlaying = false;
            }

            SendStatsUpdate();
            Clients.Client(Context.ConnectionId).registerComplete();
        }

        public void Play(int position)
        {
            // Find the game where there is a player1 and player2 and either of them have the current connection id
            var game = games.FirstOrDefault(x => x.Player1.ConnectionId == Context.ConnectionId || x.Player2.ConnectionId == Context.ConnectionId);

            if (game == null || game.IsGameOver) return;

            int marker = 0;

            //Find out if the connected player is player one or two
            if (game.Player2.ConnectionId == Context.ConnectionId)
            {
                marker = 1;
            }
            var player = marker == 0 ? game.Player1 : game.Player2;

            //return if the player is waiting for opponent but still tried to make a move
            if (player.WaitingForMove) return;

            //Notify both player that a marker has benn placed
            Clients.Client(game.Player1.ConnectionId).addMarkerPlacement(new GameInformation { OpponentName = player.Name, MarkerPosition = position });
            Clients.Client(game.Player2.ConnectionId).addMarkerPlacement(new GameInformation { OpponentName = player.Name, MarkerPosition = position });

            //Place marker and check for a winner
            if (game.Play(marker, position))
            {
                games.Remove(game);

                Clients.Client(game.Player1.ConnectionId).gameOver(player.Name);
                Clients.Client(game.Player2.ConnectionId).gameOver(player.Name);
            }

            //Notify the players that the game is over
            if (game.IsGameOver && game.IsDraw)
            {
                games.Remove(game);
                _gamesPlayed += 1;
                Clients.Client(game.Player1.ConnectionId).gameOver("It's a draw!");
                Clients.Client(game.Player2.ConnectionId).gameOver("It's a draw!");
            }

            if (!game.IsGameOver)
            {
                player.WaitingForMove = !player.WaitingForMove;
                player.Opponent.WaitingForMove = !player.Opponent.WaitingForMove;

                Clients.Client(player.Opponent.ConnectionId).waitingForMarkerPlacement(player.Name);
            }

            SendStatsUpdate();
        }

        private Random random = new Random();
        public void FindOpponent()
        {
            var player = _clients.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (player == null) return;

            player.LookingForOpponent = true;
            //Look for an opponent if there's more than one player looking for a game
            var opponent = _clients.Where(x => x.ConnectionId != Context.ConnectionId && x.LookingForOpponent && !x.IsPlaying).OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            if (opponent == null)
            {
                Clients.Client(Context.ConnectionId).noOpponents();
                return;
            }

            player.IsPlaying = true;
            player.LookingForOpponent = false;
            opponent.IsPlaying = true;
            opponent.LookingForOpponent = false;

            player.Opponent = opponent;
            opponent.Opponent = player;

            //Notify both players that a game has ben found
            Clients.Client(Context.ConnectionId).foundOpponent(opponent.Name);
            Clients.Client(opponent.ConnectionId).foundOpponent(player.Name);

            if (random.Next(0, 5000) % 2 == 0)
            {
                player.WaitingForMove = false;
                opponent.WaitingForMove = true;

                Clients.Client(player.ConnectionId).waitingForMarkerPlacement(opponent.Name);
                Clients.Client(opponent.ConnectionId).waitingForOpponent(opponent.Name);
            }
            else
            {
                player.WaitingForMove = true;
                opponent.WaitingForMove = false;

                Clients.Client(opponent.ConnectionId).waitingForMarkerPlacement(opponent.Name);
                Clients.Client(player.ConnectionId).waitingForOpponent(opponent.Name);
            }

            lock(_syncRoot)
            {
                //Add new game to games list
                games.Add(new TicTacToe { Player1 = player, Player2 = opponent });
            }
        }


    }
}