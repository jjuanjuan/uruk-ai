using Godot;
using System.Collections.Generic;

public partial class CharacterPartyPool : Node
{
    public Team Team;

    private List<CharacterParty> _available = new();
    private List<CharacterParty> _inUse = new();

    public void Setup(Team team)
    {
        Team = team;
    }

    public CharacterParty CreateParty()
    {
        var party = GameManager.I.CreateParty(Team, $"{Team.Name} Party");

        _available.Add(party);

        return party;
    }

    public CharacterParty GetFirstAvailable()
    {
        if (_available.Count == 0)
            return null;

        var party = _available[0];

        _available.RemoveAt(0);
        _inUse.Add(party);

        return party;
    }

    public void Release(CharacterParty party)
    {
        if (_inUse.Remove(party))
            _available.Add(party);
    }

    public List<CharacterParty> GetAvailable()
    {
        return _available;
    }
}