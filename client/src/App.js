import React, { Component } from "react";
import { Input, Button, Container, Header, Modal } from "semantic-ui-react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import faker from "faker";
import JoinCreateGame from "./Components/JoinCreateGame";
import Game from "./Components/Game";

export default class App extends Component {
  constructor(props) {
    super(props);

    this.state = {
      name: faker.name.firstName(),
      lobbyId: "",
      userArray: [],
      connectedToLobby: false,
      showSelectableQuestions: false,
      selectableQuestions: [],
      possibleAnswers: [],
      nameModalOpen: true,
      chat: [],
    };
  }

  componentDidMount() {
    this.connectToHub();
  }

  createGame = () => {
    this.connection.invoke("CreateGame", { rounds: 3, roundDuration: 30 });
  };

  createLobby = () => {
    this.connection.invoke("CreateLobby", this.state.name);
  };

  joinLobby = () => {
    const { name, lobbyId } = this.state;
    console.log("%cConnecting to code: ", "color: orange", lobbyId);
    this.connection.invoke("JoinLobby", name, lobbyId);
  };

  leaveLobby = () => {
    this.pushtToChat("", "You left the lobby");
    this.connection.invoke("LeaveLobby");
  };

  pushtToChat = (sender, message) => {
    const newEntry = { sender, message };
    const chat = this.state.chat;
    chat.push(newEntry);

    this.setState({ chat: chat });
  };

  connectToHub = () => {
    this.connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5000/triviaGameServer")
      .build();

    this.connection.on("broadcastMessage", (username, message) => {
      console.log(username, message);
    });

    this.connection.on("joinLobby", (joinLobbyEvent) => {
      this.pushtToChat("", "You joined the lobby");
      this.setState({
        userArray: joinLobbyEvent.usernames,
        connectedToLobby: true,
        lobbyId: joinLobbyEvent.lobbyId,
      });
    });

    this.connection.on("userJoinedLobby", (userJoinedEvent) => {
      console.log(`${userJoinedEvent.newUser} joined the lobby`);
      console.log("In the lobby are: ", userJoinedEvent.usernames);
      this.setState({ userArray: userJoinedEvent.usernames });
    });

    this.connection.on("leaveLobby", () => {
      console.log(`You have left the lobby`);
      this.setState({ connectedToLobby: false, userArray: [] });
    });

    this.connection.on("userLeftLobby", (userLeftEvent) => {
      console.log(`${userLeftEvent.leavingUser} has left the lobby`);
      console.log("In the lobby are: ", userLeftEvent.usernames);

      this.setState({ userArray: userLeftEvent.usernames });
    });

    // TODO : Handle events correctly
    this.connection.on("gameStarted", () => {
      console.log(`game started`);
    });

    this.connection.on("showCategories", (showCategoriesEvent) => {
      console.log(
        `user ${showCategoriesEvent.username} is choosing a category`
      );
      console.log("Categories:", showCategoriesEvent.categories);

      let showSelectableQuestions = false;

      if (this.state.name == showCategoriesEvent.username) {
        showSelectableQuestions = true;
      }
      this.setState({
        selectableQuestions: showCategoriesEvent.categories,
        showSelectableQuestions,
      });

      console.log(showCategoriesEvent.categories);

      /*if(this.state.name == showCategoriesEvent.username) {
        this.connection.invoke("CategorySelected", "Hausparty")
      }*/
    });

    this.connection.on("showQuestion", (showQuestionEvent) => {
      this.setState({ possibleAnswers: showQuestionEvent.answers });

      //this.connection.invoke("AnswerSelected", "aha")
    });

    this.connection.on("updatePoints", (updatePointsEvent) => {
      console.log(updatePointsEvent.points);

      this.setState({ points: updatePointsEvent.points });
    });

    this.connection.on("showFinalResult", (showFinalResultEvent) => {
      console.log(showFinalResultEvent.points);
    });

    // ----------------------------

    this.connection
      .start()
      .then(
        (resolve) => {
          console.log(resolve);
        },
        (reject) => {
          console.log(reject);
        }
      )
      .catch((error) => {
        console.log(error);
      });
  };

  handleInputChange = (e, props) => {
    const { name, value } = props;
    if (this.state.connectedToLobby) return;
    const newState = this.state;
    newState[name] = value;
    this.setState(newState);
  };

  handleAnswerSelect = (answer) => {
    this.connection.invoke("AnswerSelected", answer);
    this.setState({ possibleAnswers: [] });
  };

  handleQuestionSelect = (question) => {
    this.connection.invoke("CategorySelected", question);
    this.setState({ selectableQuestions: [] });
  };

  closeModal = () => {
    this.setState({ nameModalOpen: false });
  };

  render() {
    const { connectedToLobby, lobbyId, name, chat } = this.state;

    return (
      <Container>
        <Header as="h1">Wilkommen bei Ultimate Trivia</Header>

        {!connectedToLobby && (
          <JoinCreateGame
            lobbyId={lobbyId}
            joinLobby={this.joinLobby}
            leaveLobby={this.leaveLobby}
            createLobby={this.createLobby}
            connectedToLobby={connectedToLobby}
            handleInputChange={this.handleInputChange}
          />
        )}

        {connectedToLobby && (
          <Game
            selectableQuestions={this.state.selectableQuestions}
            handleQuestionSelect={this.handleQuestionSelect}
            handleAnswerSelect={this.handleAnswerSelect}
            possibleAnswers={this.state.possibleAnswers}
            userArray={this.state.userArray}
            points={this.state.points}
            createGame={this.createGame}
            leaveLobby={this.leaveLobby}
            name={name}
            chat={chat}
          />
        )}

        <h1 style={{ width: "100%", textAlign: "center" }}>{lobbyId}</h1>

        <Modal size={"mini"} open={this.state.nameModalOpen}>
          <Modal.Header>Gib einen Namen ein</Modal.Header>
          <Modal.Content>
            <p>Zum Spielen brauchst du einen Namen</p>
            <Input
              fluid
              name={"name"}
              action={{ icon: "rocket", onClick: this.closeModal }}
              value={this.state.name}
              onChange={this.handleInputChange}
            />
          </Modal.Content>
        </Modal>
      </Container>
    );
  }
}
