namespace Myriad;

class Timer {
    public double Lifetime { get; set; }
    public Action Action { get; private set; }
    public bool Repeat { get; private set; }
    public bool Active { get; private set; }
    public double Time { get; private set; }
    public int TimesFired { get; private set; }

    public bool Done { get { return Time >= Lifetime; } }

    public Timer(double lifetime, Action action, bool start=true, bool repeat=false, bool fire=false) {
        Lifetime = lifetime;
        Action = action;

        Active = false;
        if (start)
            Start();

        Repeat = repeat;

        if (Active && fire) {
            Fire();
        }

    }

    // If active, check if the timer is done and fire the action when it is
    public void Tick(float delta) {
        if (!Active) {
            return;
        }

        Time += delta;

        if (Done) {
            Fire();
            TimesFired++;
        }
    }

    // Start the timer (and reset it)
    public void Start() {
        Time = 0.0;
        Active = true;
    }

    // Stop the timer
    public void Stop() {
        Active = false;
    }

    // Toggle the timer on and off
    public void Toggle() {
        Active = !Active;
    }

    // Invoke the timer's action
    public void Fire() {
        Action.Invoke();
        if (Repeat) {
            Start();
        } else {
            Stop();
        }
    }
}
