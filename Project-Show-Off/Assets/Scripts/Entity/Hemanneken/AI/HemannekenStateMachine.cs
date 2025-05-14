
public class HemannekenStateMachine : StateMachine
{
    protected override State InitialState => new HemannekenRoamingState(this);

}
