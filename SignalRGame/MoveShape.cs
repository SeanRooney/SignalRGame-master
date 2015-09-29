using System;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;

namespace MoveShape
{
    public class Broadcaster
    {
        private readonly static Lazy<Broadcaster> _instance =
            new Lazy<Broadcaster>(() => new Broadcaster());
        // Send message to clients 25 times every second
        private readonly TimeSpan BroadcastInterval =
            TimeSpan.FromMilliseconds(40);
        private readonly IHubContext _hubContext;
        private Timer _broadcastLoop;
        private ShapeModel _model;
        private bool _modelUpdated;
        //public Broadcaster()
        //{
        //    _hubContext = GlobalHost.ConnectionManager.GetHubContext<MoveShapeHub>();
        //    _model = new ShapeModel();
        //    _modelUpdated = false;
        //    _broadcastLoop = new Timer(
        //        BroadcastShape,
        //        null,
        //        BroadcastInterval,
        //        BroadcastInterval);
        //}
        //public void BroadcastShape(object state)
        //{
        //    if (_modelUpdated)
        //    {
        //        _hubContext.Clients.AllExcept(_model.LastUpdatedBy).updateShape(_model);
        //        _modelUpdated = false;
        //    }
        //}
        public void UpdateShape(ShapeModel clientModel)
        {
            _model = clientModel;
            _modelUpdated = true;
        }
        public static Broadcaster Instance
        {
            get
            {
                return _instance.Value;
            }
        }
    }

    public class MoveShapeHub : Hub
    {
        private Broadcaster _broadcaster;
        public MoveShapeHub()
            : this(Broadcaster.Instance)
        {
        }
        public MoveShapeHub(Broadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }
        public void UpdateModel(ShapeModel clientModel)
        {
            clientModel.LastUpdatedBy = Context.ConnectionId;
            _broadcaster.UpdateShape(clientModel);
        }
    }
    public class ShapeModel
    {
        [JsonProperty("left")]
        public double Left { get; set; }
        [JsonProperty("top")]
        public double Top { get; set; }
        [JsonIgnore]
        public string LastUpdatedBy { get; set; }
    }
}