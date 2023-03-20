import { DoDelete } from "@/helpers/webFetchHelper";
import { AppShell, Burger, Button, createStyles, Grid, Group, Header, MediaQuery, Navbar, NavLink, rem } from "@mantine/core";
import { signOut, useSession } from "next-auth/react";
import { AppProps } from "next/app";
import Link from "next/link";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";

const useStyles = createStyles((theme) => ({
    header: {
        backgroundColor: theme.colors.dark[6],
        paddingBottom: 0,
    },
    navbar: {
        backgroundColor: theme.colors.dark[6],
        paddingBottom: 0,
      },
    mainLinks: {
        fontWeight: "bold",
        '&:hover': {
            backgroundColor: theme.colors.dark[5],
            color: theme.colorScheme === 'dark' ? theme.white : theme.black
        }
    },
    subLinks: {
        '&:hover': {
            backgroundColor: theme.colors.dark[5],
            color: theme.colorScheme === 'dark' ? theme.white : theme.black
        }
    }
}));

const Navigation = (props: AppProps) => {
    const { Component, pageProps } = props;
    const { classes } = useStyles();
    const { data: session, status } = useSession();
    const [burgerOpened, setBurgerOpened] = useState(false);
    const [windowPathName, setWindowPathName] = useState("");

    const LogoutUser = (sessionId: string) => {
        signOut();
        DoDelete('/api/auth/DeleteUserSession', sessionId);
    
        toast.success('Succesfully logged out', {
            position: 'bottom-right',
            closeOnClick: true,
            theme: 'colored'
        });
    }

    useEffect(() => {
        console.log(window.location.pathname);
        setWindowPathName(window.location.pathname);
    }, [])

    return (
        <>
            <AppShell
                header={
                    <Header height={60} className={classes.header}>
                        <Grid>
                            <Grid.Col span={6}>
                                { /* do not display when it's its larger than sm */}
                                <MediaQuery largerThan="sm" styles={{ display: 'none' }}>
                                    <Burger
                                        opened={burgerOpened}
                                        onClick={() => setBurgerOpened((o) => !o)}
                                        size="sm"
                                        mr="xl"
                                        style={{ marginTop: 15, marginRight: 20 }}
                                    />
                                </MediaQuery>
                            </Grid.Col>
                            <Grid.Col span={6} sx={{ display: 'flex', justifyContent: 'right' }}>
                                <Button sx={{ marginTop: 15, marginRight: 10 }}>
                                    Login
                                </Button>
                                <Button sx={{ marginTop: 15, marginRight: 20 }}>
                                    Register
                                </Button>
                            </Grid.Col>
                        </Grid>
                    </Header>
                    }
                    navbar={
                        <Navbar hiddenBreakpoint="sm" hidden={!burgerOpened} width={{ sm: 150, lg: 150 }} className={classes.navbar}>
                            <Navbar.Section>
                                <Link href='/' passHref style={{ textDecoration: 'none' }}>
                                    <NavLink label='Home' className={classes.mainLinks} active={'/' === windowPathName} onClick={() => setWindowPathName('/')}/>
                                </Link>
                                <Link href='/albums' passHref style={{ textDecoration: 'none' }}>
                                    <NavLink label='Albums' className={classes.mainLinks} active={'/albums' === windowPathName} onClick={() => setWindowPathName('/albums')}/>
                                </Link>
                                <Link href='/users' passHref style={{ textDecoration: 'none' }}>
                                    <NavLink label='Users' className={classes.mainLinks} active={'/users' === windowPathName} onClick={() => setWindowPathName('/users')}/>
                                </Link>
                            </Navbar.Section>
                        </Navbar>
                    }
            >
                <Component {...pageProps} />
            </AppShell>
        </>
    );
}

export default Navigation;