export class HUB {
    constructor(connection) {
        this.connection = connection
    }

    sendMessage = message => {
        this.connection.invoke('SendMessage', message)
    }

    createGame = () => {
        this.connection.invoke('StartGame', { rounds: 3, answerDuration: 30 })
    }

    createLobby = name => {
        this.connection.invoke('CreateLobby', name)
    }

    joinLobby = ({ name, lobbyId }) => {
        console.log('%cConnecting to code: ', 'color: orange', lobbyId)
        this.connection.invoke('JoinLobby', name, lobbyId)
    }
}
